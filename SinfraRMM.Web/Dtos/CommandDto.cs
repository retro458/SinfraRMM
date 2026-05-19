namespace SinfraRMM.Web.Dtos
{
    public class CommandDto
    {
        public int Id {get;set;}
        public string Name {get;set;} = string.Empty;
        public string? Description {get;set;}
        public bool RequiresAdmin {get;set;}
        public string? Category {get;set;}
    }

    public class CommandHistoryDto
    {
        public int Id {get;set;}
        public string Command {get;set;} = string.Empty;
        public string? ActionOutput {get;set;}
        public string Status {get;set;} = string.Empty;
        public DateTime CreatedAt {get;set;}
    }

    public class CreateCommandDto
    {
        public string Name {get;set;} = string.Empty;
        public string ActualCommand {get;set;} = string.Empty;
        public string Description {get;set;} = string.Empty;
        public bool RequieresAdmin {get;set;}

        public string? Category {get;set;}
    }
}