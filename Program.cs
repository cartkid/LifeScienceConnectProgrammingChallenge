using System.Text;
using Models;
using Newtonsoft.Json;

await DoWorkAsync();

static async Task DoWorkAsync()
{
    Dictionary<string, List<int>> categoriesToMagazinesCache = new Dictionary<string, List<int>>();

    var tokenObj = await GetToken();
    if (tokenObj == null || !tokenObj.success)
    {
        Console.WriteLine("Error: Could not obtain a token.");
        return;
    }

    var allSubscribersTask = GetSubscribers(tokenObj.token);
    var allCategoriesTask = GetCategories(tokenObj.token);
    await Task.WhenAll(allSubscribersTask, allCategoriesTask);
    var allSubscribers = await allSubscribersTask;
    var allCategories = await allCategoriesTask;

    List<Task<Models.ApiResponseMagazines>> magazinesInCategoryTasks = new List<Task<Models.ApiResponseMagazines>>();
    foreach (var category in allCategories.data)
    {
        categoriesToMagazinesCache.Add(category, new List<int>());
        magazinesInCategoryTasks.Add(GetMagazinesInCategory(tokenObj.token, category));
    }

    IEnumerable<Models.ApiResponseMagazines> magazinesInCategoryTasksResults = await Task.WhenAll<Models.ApiResponseMagazines>(magazinesInCategoryTasks);
    foreach (var magazinesInCategory in magazinesInCategoryTasksResults)
    {
        foreach (var magazineInCategory in magazinesInCategory.data)
        {
            if (categoriesToMagazinesCache.Any(x => x.Key == magazineInCategory.category))
                categoriesToMagazinesCache[magazineInCategory.category].Add(magazineInCategory.id);
            else
                categoriesToMagazinesCache[magazineInCategory.category] = new List<int>() { magazineInCategory.id };
        }
    }

    Models.Answer answer = new Models.Answer();
    foreach (var subscriber in allSubscribers.data)
    {
        if (subscriberSubscribedToAtLeastOneMagazineInEachCategory(categoriesToMagazinesCache, subscriber))
            answer.subscribers.Add(subscriber.id);
    }

    // Post Answer
    var answerResult = await PostAnswer(tokenObj.token, answer);
    Console.WriteLine(string.Format("totalTime: {0}", answerResult.data.totalTime));
    Console.WriteLine(string.Format("answerCorrect: {0}", answerResult.data.answerCorrect ? "true" : "false"));
    if (!answerResult.data.answerCorrect)
        Console.WriteLine(string.Format("shouldBe: {0}", answerResult.data.shouldBe != null ? string.Join(", ", answerResult.data.shouldBe) : ""));
    Console.WriteLine("Exiting now.");
}

static bool subscriberSubscribedToAtLeastOneMagazineInEachCategory(Dictionary<string, List<int>> categoriesToMagazinesCache, Subscriber subscriber)
{
    foreach (var category in categoriesToMagazinesCache)
    {
        if (!subscriber.magazineIds.Intersect(category.Value).Any())
            return false;
    }
    return true;
}

static async Task<Models.ApiResponse> GetToken()
{
    const string REQUEST_URL = "/api/token";
    Models.ApiResponse result = await GetRequest<Models.ApiResponse>(REQUEST_URL);
    return result;
}

static async Task<Models.ApiResponseSubscribers> GetSubscribers(string token)
{
    const string REQUEST_URL = "/api/subscribers/{0}";
    string request_url = string.Format(REQUEST_URL, token);
    Models.ApiResponseSubscribers result = await GetRequest<Models.ApiResponseSubscribers>(request_url);
    return result;
}

static async Task<Models.ApiResponseCategories> GetCategories(string token)
{
    const string REQUEST_URL = "/api/categories/{0}";
    string request_url = string.Format(REQUEST_URL, token);
    Models.ApiResponseCategories result = await GetRequest<Models.ApiResponseCategories>(request_url);
    return result;
}

static async Task<Models.ApiResponseMagazines> GetMagazinesInCategory(string token, string category)
{
    const string REQUEST_URL = "/api/magazines/{0}/{1}";
    string request_url = string.Format(REQUEST_URL, token, category);
    Models.ApiResponseMagazines result = await GetRequest<Models.ApiResponseMagazines>(request_url);
    return result;
}

static async Task<Models.ApiResponseAnswerResponse> PostAnswer(string token, Models.Answer answer)
{
    const string REQUEST_URL = "/api/answer/{0}";
    string request_url = string.Format(REQUEST_URL, token);
    Models.ApiResponseAnswerResponse result = await PostRequest<Models.ApiResponseAnswerResponse>(request_url, answer);
    return result;
}

static async Task<T> GetRequest<T>(string relativeUrl)
{
    Uri baseUri = new Uri("http://magazinestore.azurewebsites.net");
    Uri fullUrl = new Uri(baseUri, relativeUrl);
    T? returnMe = default(T);
    using (HttpClient client = new HttpClient())
    {
        client.DefaultRequestHeaders.Accept.Clear();
        var response = await client.GetStringAsync(fullUrl);
        if (response != null)
            returnMe = JsonConvert.DeserializeObject<T>(response);
    }
    return returnMe;
}

static async Task<T> PostRequest<T>(string relativeUrl, Models.Answer answer)
{
    Uri baseUri = new Uri("http://magazinestore.azurewebsites.net");
    Uri fullUrl = new Uri(baseUri, relativeUrl);
    T? returnMe = default(T);
    using (HttpClient client = new HttpClient())
    {
        client.DefaultRequestHeaders.Accept.Clear();
        var payload = System.Text.Json.JsonSerializer.Serialize(answer);
        HttpContent httpContent = new StringContent(payload, Encoding.UTF8, "application/json");
        var response = await client.PostAsync(fullUrl, httpContent);
        if (response.IsSuccessStatusCode)
            returnMe = JsonConvert.DeserializeObject<T>(await response?.Content?.ReadAsStringAsync());
    }
    return returnMe;
}

