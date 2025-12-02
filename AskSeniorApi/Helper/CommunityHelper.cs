using AskSeniorApi.Models;
using Supabase;
using Supabase.Postgrest.Exceptions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AskSeniorApi.Helper
{
    public class CommunityService
    {
        private readonly Supabase.Client _client;

        public CommunityService(Supabase.Client client)
        {
            _client = client;
        }

        public async Task<bool> LinkCommunityToTopics(
            string communityId,
            IEnumerable<string> topicIds
        )
        {
            try
            {
                var inserts = topicIds.Select(topicId => new CommunityTopic
                {
                    CommunityId = communityId,
                    TopicId = topicId
                }).ToList();

                var result = await _client.From<CommunityTopic>().Insert(inserts);

                return result.Models.Any();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error linking topics: {ex.Message}");
                return false;
            }
        }
    }


}
