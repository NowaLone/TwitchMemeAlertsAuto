using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace TwitchMemeAlertsAuto.Core
{
	public static class StickerSearchUtility
	{
		private const double ExactWeight = 100;
		private const double PrefixWeight = 70;
		private const double SubstringWeight = 35;
		private const double FuzzyWeight = 15;

		private const double ExactNamePhraseWeight = 5000;
		private const double ExactDescriptionPhraseWeight = 4500;
		private const double ExactStreamerPhraseWeight = 3500;
		private const double ExactTagPhraseWeight = 3000;

		private const double NameSequenceWeight = 500;
		private const double DescriptionSequenceWeight = 150;

		private const double CoverageWeight = 300;

		public static Sticker? FindBest(this
			IEnumerable<Sticker> stickers,
			string query)
		{
			if (stickers == null)
				throw new ArgumentNullException(nameof(stickers));

			if (string.IsNullOrWhiteSpace(query))
				return null;

			var items = stickers
				.Where(x => x != null)
				.ToList();

			if (items.Count == 0)
				return null;

			var normalizedQuery = Normalize(query);

			if (string.IsNullOrWhiteSpace(normalizedQuery))
				return null;

			var queryTokens = Tokenize(normalizedQuery);

			if (queryTokens.Count == 0)
				return null;

			return items
				.Select(sticker => new SearchResult
				{
					Sticker = sticker,
					Score = CalculateScore(
						sticker,
						normalizedQuery,
						queryTokens)
				})
				.OrderByDescending(x => x.Score)
				.Select(x => x.Sticker)
				.FirstOrDefault();
		}

		private static double CalculateScore(
			Sticker sticker,
			string query,
			IReadOnlyList<string> queryTokens)
		{
			var name = Normalize(sticker.Name);
			var description = Normalize(sticker.Description);
			var music = Normalize(sticker.Music);
			var streamer = Normalize(sticker.StreamerName);

			var tags = sticker.Tags?
				.Select(x => Normalize(x?.Name))
				.Where(x => !string.IsNullOrWhiteSpace(x))
				.ToList()
				?? new List<string>();

			double score = 0;

			// Exact full-name match.
			if (name == query)
			{
				score += ExactNamePhraseWeight;
			}
			else if (name.Contains(
						 query,
						 StringComparison.Ordinal))
			{
				score += ExactNamePhraseWeight;
			}

			// Exact full-query match in description.
			if (!string.IsNullOrWhiteSpace(description) &&
				description.Contains(
					query,
					StringComparison.Ordinal))
			{
				score += ExactDescriptionPhraseWeight;
			}

			// Exact full-query match in streamer name.
			if (!string.IsNullOrWhiteSpace(streamer) &&
				streamer.Contains(
					query,
					StringComparison.Ordinal))
			{
				score += ExactStreamerPhraseWeight;
			}

			// Exact full-query match in a tag.
			if (tags.Any(tag =>
					tag.Contains(
						query,
						StringComparison.Ordinal)))
			{
				score += ExactTagPhraseWeight;
			}

			//
			// Score individual tokens.
			//
			foreach (var token in queryTokens)
			{
				score += ScoreField(
					token,
					name,
					12);

				score += ScoreField(
					token,
					description,
					4);

				score += ScoreField(
					token,
					streamer,
					10);

				foreach (var tag in tags)
				{
					score += ScoreField(
						token,
						tag,
						8);
				}

				score += ScoreFuzzy(
					token,
					name,
					12);

				score += ScoreFuzzy(
					token,
					description,
					4);

				score += ScoreFuzzy(
					token,
					streamer,
					10);

				foreach (var tag in tags)
				{
					score += ScoreFuzzy(
						token,
						tag,
						8);
				}
			}

			//
			// Query coverage.
			//
			var matchedTokens = queryTokens
				.Distinct(StringComparer.Ordinal)
				.Count(token =>
					MatchesAnywhere(
						token,
						name,
						description,
						streamer,
						tags));

			if (queryTokens.Count > 0)
			{
				var coverage =
					(double)matchedTokens /
					queryTokens.Count;

				score += coverage * CoverageWeight;
			}

			//
			// Reward consecutive query tokens in the name.
			//
			var nameSequenceLength =
				GetLongestSequenceLength(
					name,
					queryTokens);

			score += CalculateSequenceScore(
				nameSequenceLength,
				queryTokens.Count,
				NameSequenceWeight);

			//
			// Reward consecutive query tokens in the description.
			//
			var descriptionSequenceLength =
				GetLongestSequenceLength(
					description,
					queryTokens);

			score += CalculateSequenceScore(
				descriptionSequenceLength,
				queryTokens.Count,
				DescriptionSequenceWeight);

			return score;
		}

		private static double CalculateSequenceScore(
			int sequenceLength,
			int queryLength,
			double maxWeight)
		{
			if (sequenceLength <= 1 ||
				queryLength <= 1)
			{
				return 0;
			}

			var coverage =
				(double)sequenceLength /
				queryLength;

			return maxWeight *
				   Math.Pow(coverage, 2);
		}

		private static double ScoreField(
			string queryToken,
			string text,
			int fieldWeight)
		{
			if (string.IsNullOrWhiteSpace(text))
				return 0;

			var words = Tokenize(text);

			double best = 0;

			foreach (var word in words)
			{
				double current;

				if (word == queryToken)
				{
					current = ExactWeight;
				}
				else if (word.StartsWith(
							 queryToken,
							 StringComparison.Ordinal))
				{
					current = PrefixWeight;
				}
				else if (word.Contains(
							 queryToken,
							 StringComparison.Ordinal))
				{
					current = SubstringWeight;
				}
				else
				{
					continue;
				}

				best = Math.Max(best, current);
			}

			return best * fieldWeight;
		}

		private static double ScoreFuzzy(
			string queryToken,
			string text,
			int fieldWeight)
		{
			if (string.IsNullOrWhiteSpace(text))
				return 0;

			var words = Tokenize(text);

			// Fuzzy matching is only a fallback.
			// Never use it when an exact/prefix/substring match exists.
			if (words.Any(word =>
					word == queryToken ||
					word.StartsWith(
						queryToken,
						StringComparison.Ordinal) ||
					word.Contains(
						queryToken,
						StringComparison.Ordinal)))
			{
				return 0;
			}

			var bestSimilarity = words
				.Select(word => Similarity(
					queryToken,
					word))
				.DefaultIfEmpty(0)
				.Max();

			if (bestSimilarity < 0.75)
				return 0;

			return bestSimilarity *
				   FuzzyWeight *
				   fieldWeight;
		}

		private static int GetLongestSequenceLength(
			string text,
			IReadOnlyList<string> queryTokens)
		{
			if (string.IsNullOrWhiteSpace(text) ||
				queryTokens.Count == 0)
			{
				return 0;
			}

			var words = Tokenize(text);

			if (words.Count == 0)
				return 0;

			var longest = 0;

			for (var start = 0;
				 start < queryTokens.Count;
				 start++)
			{
				for (var length = 2;
					 start + length <= queryTokens.Count;
					 length++)
				{
					var sequence = queryTokens
						.Skip(start)
						.Take(length)
						.ToList();

					if (ContainsSequence(
							words,
							sequence))
					{
						longest = Math.Max(
							longest,
							length);
					}
				}
			}

			return longest;
		}

		private static bool ContainsSequence(
			IReadOnlyList<string> words,
			IReadOnlyList<string> queryTokens)
		{
			if (queryTokens.Count == 0 ||
				words.Count < queryTokens.Count)
			{
				return false;
			}

			for (var i = 0;
				 i <= words.Count - queryTokens.Count;
				 i++)
			{
				var matched = true;

				for (var j = 0;
					 j < queryTokens.Count;
					 j++)
				{
					var word = words[i + j];
					var token = queryTokens[j];

					if (word == token)
						continue;

					if (word.StartsWith(
							token,
							StringComparison.Ordinal))
					{
						continue;
					}

					matched = false;
					break;
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
			if (HasMatch(token, name))
				return true;

			if (HasMatch(token, description))
				return true;

			if (HasMatch(token, streamer))
				return true;

			return tags.Any(tag =>
				HasMatch(token, tag));
		}

		private static bool HasMatch(
			string token,
			string text)
		{
			if (string.IsNullOrWhiteSpace(text))
				return false;

			return Tokenize(text).Any(word =>
				word == token ||
				word.StartsWith(
					token,
					StringComparison.Ordinal) ||
				word.Contains(
					token,
					StringComparison.Ordinal));
		}

		private static double Similarity(
			string a,
			string b)
		{
			if (a == b)
				return 1;

			if (a.Length < 3 ||
				b.Length < 3)
			{
				return 0;
			}

			var distance =
				LevenshteinDistance(a, b);

			return 1.0 -
				   (double)distance /
				   Math.Max(a.Length, b.Length);
		}

		private static int LevenshteinDistance(
			string a,
			string b)
		{
			var previous =
				new int[b.Length + 1];

			var current =
				new int[b.Length + 1];

			for (var j = 0;
				 j <= b.Length;
				 j++)
			{
				previous[j] = j;
			}

			for (var i = 1;
				 i <= a.Length;
				 i++)
			{
				current[0] = i;

				for (var j = 1;
					 j <= b.Length;
					 j++)
				{
					var cost =
						a[i - 1] == b[j - 1]
							? 0
							: 1;

					current[j] = Math.Min(
						Math.Min(
							current[j - 1] + 1,
							previous[j] + 1),
						previous[j - 1] + cost);
				}

				(previous, current) =
					(current, previous);
			}

			return previous[b.Length];
		}

		private static List<string> Tokenize(
			string text)
		{
			if (string.IsNullOrWhiteSpace(text))
				return new List<string>();

			return Regex
				.Matches(
					text,
					@"[\p{L}\p{N}]+")
				.Select(x => x.Value)
				.ToList();
		}

		private static string Normalize(
			string? value)
		{
			if (string.IsNullOrWhiteSpace(value))
				return string.Empty;

			value = value.ToLowerInvariant();

			// Split hyphenated words.
			value = value.Replace("-", " ");

			// Keep only letters, numbers and whitespace.
			value = Regex.Replace(
				value,
				@"[^\p{L}\p{N}\s]",
				" ");

			// Collapse consecutive whitespace.
			value = Regex.Replace(
				value,
				@"\s+",
				" ");

			return value.Trim();
		}

		private sealed class SearchResult
		{
			public Sticker Sticker { get; init; } = null!;
			public double Score { get; init; }
		}
	}
}