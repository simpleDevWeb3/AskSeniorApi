using AskSeniorApi.DTO;
using AskSeniorApi.Models;
using Microsoft.AspNetCore.Mvc;
using Supabase;



namespace AskSeniorApi.Controllers
{
   


    [Route("api/[controller]")]
    [ApiController]
    public class MemberController : ControllerBase
    {

        private readonly Client _supabaseClient;

        public MemberController(Client supabaseClient)
        {
            _supabaseClient = supabaseClient;
        }

        [HttpGet]
        public async Task<IActionResult> GetAllGroupMember()
        {
            var members = await _supabaseClient.From<Member>().Get();

            var dtoData = members.Models.Select(m => new MemberDto
            {
                user_id = m.user_id,
                community_id = m.community_id,
                created_at = m.created_at,
                status = m.status
            }).ToList();

            return Ok(dtoData);
        }


    }
}
