using SinfraRMM.Web.Dtos;

namespace SinfraRMM.Web.Models
{
    public class ServerDetailViewModel
    {
        public ServerDto Server {get;set;} = null!; 
        public List<CommandHistoryDto> History {get;set;} = new();
    }

   
}