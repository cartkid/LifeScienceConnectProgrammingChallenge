
namespace Models
{
    public class Answer
    {
        public Answer()
        {
            this.subscribers = new List<string>();
        }

        public List<string> subscribers { get; set; }
    }
}