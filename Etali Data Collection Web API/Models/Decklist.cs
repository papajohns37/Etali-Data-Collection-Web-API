namespace Etali_Data_Collection_Web_API.Models
{
    public class Decklist
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string[] CardNames { get; set; } = Array.Empty<string>();
    }
}
