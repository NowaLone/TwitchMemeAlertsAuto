using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;

namespace TwitchMemeAlertsAuto.Core.Tests;

[TestClass]
[TestCategory(nameof(StickerSearchUtility))]
public class StickerSearchUtilityTests
{
	private static readonly string TestDataPath = Path.Combine(AppContext.BaseDirectory, "TestData");

	[TestMethod]
	[DynamicData(nameof(SearchCases))]
	[TestCategory(nameof(StickerSearchUtility.FindBest))]
	public void FindBest_ReturnsExpectedSticker(string jsonFile, string phrase, string expectedId)
	{
		var json = File.ReadAllText(Path.Combine(TestDataPath, jsonFile));
		var stickers = JsonSerializer.Deserialize(json, SerializationModeOptionsContext.Default.ListSticker);

		Assert.IsNotNull(stickers);

		var result = stickers.FindBest(phrase);

		Assert.IsNotNull(result);
		Assert.AreEqual(expectedId, result.Id);
	}

	private static IEnumerable<object[]> SearchCases()
	{
		yield return new object[] { "1.json", "всем похуй влг", "654c0ebd5ee664ab07b28fe6" };
		yield return new object[] { "2.json", "Аниме-косплей фестиваль", "6a67de9f01ab0d6f313f1988" };
		yield return new object[] { "3.json", "я победил nuke", "657c0e13422c4bf13b04c0b8" };
		yield return new object[] { "4.json", "иди в жопу дерьмо очкастое", "69ab51d546c2e18f5e6480cc" };
		yield return new object[] { "5.json", "можно вас укусить за", "695b0139e26898bbf64d9d36" };
		yield return new object[] { "5.json", "morgpie можно вас укусить", "695b0139e26898bbf64d9d36" };
		yield return new object[] { "5.json", "nowarualone можно вас укусить", "695b0139e26898bbf64d9d36" };
		yield return new object[] { "5.json", "nowaru можно вас укусить", "695b0139e26898bbf64d9d36" };
		yield return new object[] { "6.json", "bukabyak такую только", "69765f14b16b1c4d7316c7ea" };
		yield return new object[] { "7.json", "Blxcknoise ня", "6a761ee5012c369ead541990" };
		yield return new object[] { "7.json", "ня Blxcknoise", "6a761ee5012c369ead541990" };
	}
}