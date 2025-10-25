using EchoSpace.Core.DTOs;
using EchoSpace.Core.Entities;
using EchoSpace.Core.Interfaces;
using Microsoft.EntityFrameworkCore;
using EchoSpace.Infrastructure.Data;

namespace EchoSpace.Infrastructure.Services
{
    public class SearchService : ISearchService
    {
        private readonly EchoSpaceDbContext _context;

        public SearchService(EchoSpaceDbContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<SearchResultDto>> SearchUsersAsync(string query, int limit = 10)
        {
            if (string.IsNullOrWhiteSpace(query))
            {
                return Enumerable.Empty<SearchResultDto>();
            }

            // Get all users from database
            var users = await _context.Users
                .Select(u => new
                {
                    u.Id,
                    u.Name,
                    u.UserName,
                    u.Email
                })
                .ToListAsync();

            // Calculate similarity scores for each user
            var results = users
                .Select(u => new
                {
                    User = u,
                    NameScore = CalculateSimilarity(query, u.Name),
                    UserNameScore = CalculateSimilarity(query, u.UserName),
                    EmailScore = CalculateSimilarity(query, u.Email)
                })
                .Select(x => new SearchResultDto
                {
                    Id = x.User.Id,
                    Name = x.User.Name,
                    UserName = x.User.UserName,
                    Email = x.User.Email,
                    MatchScore = Math.Max(Math.Max(x.NameScore, x.UserNameScore), x.EmailScore)
                })
                .Where(r => r.MatchScore >= 0.7) // 70% threshold
                .OrderByDescending(r => r.MatchScore)
                .Take(limit)
                .ToList();

            return results;
        }

        /// <summary>
        /// Calculate similarity between two strings using Levenshtein distance.
        /// Returns a score between 0 (no match) and 1 (perfect match).
        /// </summary>
        private double CalculateSimilarity(string query, string target)
        {
            if (string.IsNullOrWhiteSpace(query) || string.IsNullOrWhiteSpace(target))
            {
                return 0;
            }

            // Normalize strings: lowercase and trim
            query = query.ToLower().Trim();
            target = target.ToLower().Trim();

            // Check for exact match
            if (query == target)
            {
                return 1.0;
            }

            // Check if target contains query (substring match)
            if (target.Contains(query))
            {
                return 0.95;
            }

            // Calculate Levenshtein distance
            int distance = LevenshteinDistance(query, target);
            int maxLength = Math.Max(query.Length, target.Length);

            // Convert distance to similarity score (0 to 1)
            double similarity = 1.0 - ((double)distance / maxLength);

            return similarity;
        }

        /// <summary>
        /// Calculate Levenshtein distance between two strings.
        /// Returns the minimum number of single-character edits required to change one string into the other.
        /// </summary>
        private int LevenshteinDistance(string source, string target)
        {
            if (string.IsNullOrEmpty(source))
            {
                return string.IsNullOrEmpty(target) ? 0 : target.Length;
            }

            if (string.IsNullOrEmpty(target))
            {
                return source.Length;
            }

            int sourceLength = source.Length;
            int targetLength = target.Length;

            var distance = new int[sourceLength + 1, targetLength + 1];

            // Initialize first column and row
            for (int i = 0; i <= sourceLength; i++)
            {
                distance[i, 0] = i;
            }

            for (int j = 0; j <= targetLength; j++)
            {
                distance[0, j] = j;
            }

            // Calculate distances
            for (int i = 1; i <= sourceLength; i++)
            {
                for (int j = 1; j <= targetLength; j++)
                {
                    int cost = (source[i - 1] == target[j - 1]) ? 0 : 1;

                    distance[i, j] = Math.Min(
                        Math.Min(
                            distance[i - 1, j] + 1,      // Deletion
                            distance[i, j - 1] + 1),     // Insertion
                        distance[i - 1, j - 1] + cost);  // Substitution
                }
            }

            return distance[sourceLength, targetLength];
        }
    }
}

