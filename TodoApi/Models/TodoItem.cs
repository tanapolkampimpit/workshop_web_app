namespace TodoApi.Models
{
    public class TodoItem
    {
        public int Id { get; set; }

        public string Titel{ get; set; }

         public string? Descrition { get; set; }

        public bool IsCompleted { get; set; }

        public DateTime CreateAt { get; set; }

        
    }
}
