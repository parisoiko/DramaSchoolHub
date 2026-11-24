namespace DramaSchoolHub.Models;

public class RehearsalRoom
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public int Capacity { get; set; }
    public bool HasPiano { get; set; }
}
