using SinfraRMM.Web.Dtos;
namespace SinfraRMM.Web.Models
{
     public class ServerConsoleViewModel
    {
        public ServerDto Server {get;set;} = null!;
        public List<CommandDto> Commands {get;set;} = new ();
    }
}