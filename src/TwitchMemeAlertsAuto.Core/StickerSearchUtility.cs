using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace TwitchMemeAlertsAuto.Core
{
	public static class StickerSearchUtility
	{
		private const double NameWeight = 12;
		private const double DescriptionWeight = 2;
		private const double StreamerWeight = 10;
		private const double TagWeight = 8;

		private const double ExactMatchWeight = 100;
		private const double FuzzyMatchWeight = 15;

		private const double ExactNamePhraseWeight = 5000;
		private const double ExactDescriptionPhraseWeight = 2500;
		private const double NameSequenceWeight = 1500;
		private const double DescriptionSequenceWeight = 100;
		private const double CoverageWeight = 300;

		public static Sticker FindBest(this IEnumerable<Sticker> stickers, string query)
		{
			if (stickers == null)
				throw new ArgumentNullException(nameof(stickers));

			if (string.IsNullOrWhiteSpace(query))
				return null;

			var items = stickers.Where(x => x != null).ToList();

			if (items.Count == 0)
				return null;

			var normalizedQuery = Normalize(query);

			if (string.IsNullOrWhiteSpace(normalizedQuery))
				return null;

			var queryTokens = Tokenize(normalizedQuery);

			if (queryTokens.Count == 0)
				return null;

			return items
				.Select(sticker => new
				{
					Sticker = sticker,
					Score = CalculateScore(sticker, normalizedQuery, queryTokens)
				})
				.OrderByDescending(x => x.Score)
				.Select(x => x.Sticker)
				.FirstOrDefault();
		}

		private static double CalculateScore(Sticker sticker, string query, IReadOnlyList<string> queryTokens)
		{
			var name = Normalize(sticker.Name);
			var description = Normalize(sticker.Description);
			var streamer = Normalize(sticker.StreamerName);

			var tags = sticker.Tags?
				.Select(x => Normalize(x?.Name))
				.Where(x => !string.IsNullOrWhiteSpace(x))
				.ToList() ?? new List<string>();

			var score = 0d;

			if (name == query)
				score += ExactNamePhraseWeight;
			else if (name.Contains(query, StringComparison.Ordinal))
				score += ExactNamePhraseWeight;

			if (!string.IsNullOrWhiteSpace(description) &&
				description.Contains(query, StringComparison.Ordinal))
			{
				score += ExactDescriptionPhraseWeight;
			}

			foreach (var token in queryTokens)
			{
				score += ScoreField(token, name, NameWeight);
				score += ScoreField(token, description, DescriptionWeight);
				score += ScoreField(token, streamer, StreamerWeight);

				foreach (var tag in tags)
					score += ScoreField(token, tag, TagWeight);
			}

			var matchedTokens = queryTokens
				.Distinct(StringComparer.Ordinal)
				.Count(token => MatchesAnywhere(token, name, description, streamer, tags));

			if (queryTokens.Count > 0)
				score += (double)matchedTokens / queryTokens.Count * CoverageWeight;

			score += CalculateSequenceScore(
				GetLongestSequenceLength(name, queryTokens),
				queryTokens.Count,
				NameSequenceWeight);

			score += CalculateSequenceScore(
				GetLongestSequenceLength(description, queryTokens),
				queryTokens.Count,
				DescriptionSequenceWeight);

			return score;
		}

		private static double ScoreField(string queryToken, string text, double fieldWeight)
		{
			if (string.IsNullOrWhiteSpace(text))
				return 0;

			var words = Tokenize(text);

			if (words.Count == 0)
				return 0;

			if (words.Any(word => word == queryToken))
				return ExactMatchWeight * fieldWeight;

			var bestSimilarity = words
				.Select(word => Similarity(queryToken, word))
				.DefaultIfEmpty(0)
				.Max();

			if (bestSimilarity < 0.8)
				return 0;

			return bestSimilarity * FuzzyMatchWeight * fieldWeight;
		}

		private static double CalculateSequenceScore(int sequenceLength, int queryLength, double maxWeight)
		{
			if (sequenceLength <= 1 || queryLength <= 1)
				return 0;

			var coverage = (double)sequenceLength / queryLength;
			return maxWeight * coverage * coverage;
		}

		private static int GetLongestSequenceLength(string text, IReadOnlyList<string> queryTokens)
		{
			if (string.IsNullOrWhiteSpace(text) || queryTokens.Count < 2)
				return 0;

			var words = Tokenize(text);
			var longest = 0;

			for (var queryStart = 0; queryStart < queryTokens.Count - 1; queryStart++)
			{
				for (var length = 2; queryStart + length <= queryTokens.Count; length++)
				{
					var sequence = queryTokens.Skip(queryStart).Take(length).ToList();

					if (ContainsSequence(words, sequence))
						longest = Math.Max(longest, length);
				}
			}

			return longest;
		}

		private static bool ContainsSequence(IReadOnlyList<string> words, IReadOnlyList<string> sequence)
		{
			if (sequence.Count == 0 || words.Count < sequence.Count)
				return false;

			for (var i = 0; i <= words.Count - sequence.Count; i++)
			{
				var matched = true;

				for (var j = 0; j < sequence.Count; j++)
				{
					if (words[i + j] != sequence[j])
					{
						matched = false;
						break;
					}
				}

				if (matched)
					return true;
			}

			return false;
		}

		private static bool MatchesAnywhere(
			string token,
			string name,
			string description,
			string streamer,
			IReadOnlyCollection<string> tags)
		{
			return HasExactMatch(token, name) ||
				   HasExactMatch(token, description) ||
				   HasExactMatch(token, streamer) ||
				   tags.Any(tag => HasExactMatch(token, tag));
		}

		private static bool HasExactMatch(string token, string text)
		{
			if (string.IsNullOrWhiteSpace(text))
				return false;

			return Tokenize(text).Any(word => word == token);
		}

		private static double Similarity(string a, string b)
		{
			if (a == b)
				return 1;

			if (a.Length < 3 || b.Length < 3)
				return 0;

			var distance = LevenshteinDistance(a, b);

			return 1d - (double)distance / Math.Max(a.Length, b.Length);
		}

		private static int LevenshteinDistance(string a, string b)
		{
			var previous = new int[b.Length + 1];
			var current = new int[b.Length + 1];

			for (var j = 0; j <= b.Length; j++)
				previous[j] = j;

			for (var i = 1; i <= a.Length; i++)
			{
				current[0] = i;

				for (var j = 1; j <= b.Length; j++)
				{
					var cost = a[i - 1] == b[j - 1] ? 0 : 1;

					current[j] = Math.Min(
						Math.Min(current[j - 1] + 1, previous[j] + 1),
						previous[j - 1] + cost);
				}

				(previous, current) = (current, previous);
			}

			return previous[b.Length];
		}

		private static List<string> Tokenize(string text)
		{
			if (string.IsNullOrWhiteSpace(text))
				return new List<string>();

			return Regex
				.Matches(text, @"[\p{L}]+|[\p{N}]+")
				.Select(x => x.Value)
				.ToList();
		}

		private static string Normalize(string value)
		{
			if (string.IsNullOrWhiteSpace(value))
				return string.Empty;

			value = value.ToLowerInvariant();
			value = value.Replace("-", " ");
			value = Regex.Replace(value, @"[^\p{L}\p{N}\s]", " ");
			value = Regex.Replace(value, @"\s+", " ");

			return value.Trim();
		}
	}
}