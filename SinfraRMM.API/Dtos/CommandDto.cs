namespace SinfraRMM.API.Dtos
{
    // =====================================
    // REQUESTS DTOs
    // =====================================
    public class CreateCommandDto
    {
      public string name {get; set; } = string.Empty;
      public string actual_command {get; set; } = string.Empty;
      public string description {get; set; } = string.Empty;
      public bool requires_admin {get; set; }

    }

    // =====================================
    // RESPONSES DTOs
    // =====================================
    public class CommandDto
    {
      public int id {get; set; }
      public string name {get; set; } = string.Empty;
      public string actual_command {get; set; } = string.Empty;
      public string description {get; set; } = string.Empty;
      public bool requires_admin {get; set; }
    }
}