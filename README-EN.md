# TwitchMemeAlertsAuto
Utility for automatic distribution of meme coins for Twitch rewards

## How to use
There are 2 versions of the program: a regular (GUI) version with additional features and a simplified command-line version (CLI)

| Feature | GUI | CLI |
| --- | --- | --- |
| Automatic meme coin distribution | ✅ | ✅ |
| Meme distribution by Twitch username | ✅ | ✅ |
| Easy integration with Twitch and MemeAlerts | ✅ | ❌ |
| Convenient reward setup and mapping to automatic meme coin amounts | ✅ | ❌ |
| MemeAlerts supporters list with ability to reward directly from the program | ✅ | ❌ |
| Bulk rewarding of supporters | ✅ | ❌ |
| Convenient event log | ✅ | ❌ |
| Ability to minimize to system tray | ✅ | ❌ |
| Auto-update | ✅ | ❌ |
| Auto-start on system login | ✅ | ❌ |

Download both versions from the [latest release](https://github.com/NowaLone/TwitchMemeAlertsAuto/releases/latest)

### GUI
1. Create a reward on Twitch for distributing meme coins
2. Download tmaa-wpf.zip
3. Extract it anywhere you like
4. Inside the folder, run tmaa.exe
5. Connect the required integrations and configure them
[Detailed setup](#detailed-gui-setup)

### CLI
1. Create a reward on Twitch for distributing meme coins
2. Download tmaa.exe
3. Run `tmaa.exe --help` in your terminal
4. Read the parameter descriptions and run the program with your parameters
[Detailed setup](#detailed-cli-setup)

🚨 THE REWARD MUST HAVE AN INPUT FIELD 🚨  
🚨 THE USER MUST BE A SUPPORTER ON MEMEALERTS (PURCHASED MEMECOIN OR RECEIVED WELCOME BONUS) 🚨

## Detailed GUI setup
If you need the full functionality, you need to connect both Twitch and MemeAlerts. If you only want to use automatic distribution, connecting MemeAlerts alone is sufficient.

### Connecting Twitch
- A device activation page will open in your browser
- Click "Activate", a permissions request page will open (permissions are for reading rewards and chat)
- Grant permissions, rewards will automatically load in the program
- Enter the number of meme coins to distribute for each reward in the field below the respective reward, then click Save

### Connecting MemeAlerts
- An embedded browser will open with the MemeAlerts page where you need to log in — why is this necessary?[^1]

[^1]: Because MemeAlerts doesn't have a public authorization API (or any API at all), we have to perform a trick similar to the one described in [How to extract the MemeAlerts token?](#how-to-extract-the-memealerts-token), which cannot be done in an external browser.

- After logging in, supporters will automatically load in the program

### Small FAQ
- In the field below a supporter's name, you can enter and distribute the desired amount of meme coins (same as on the website)
- On the left there are bulk reward buttons: enter the number of meme coins to distribute in the first field, then choose a reward pattern:
  - Reward all supporters
  - Reward the last N supporters who sent memes
  - Reward the last N supporters who received meme coins
- Updates are not checked automatically, but if updates are available, you can update automatically by clicking Help → Check for updates
- Double-clicking the tray icon minimizes/restores the program

## Detailed CLI setup

### How to extract the MemeAlerts token?
1. Go to https://memealerts.com/
2. Log in
3. Open the developer console (depends on browser, usually F12 or Ctrl+Shift+I)
4. `/screenshots/image.png`
5. Type in the console:
```javascript
localStorage.getItem("accessToken");
```
6. Get the token and save it

### How to extract the Twitch reward ID?
1. Go to https://www.instafluff.tv/TwitchCustomRewardID/?channel=YOUR_CHANNEL
2. Click the desired reward on your channel
3. The reward ID should appear on the website — save it

### Final launch command example:
```
tmaa.exe "-c=justinfan123" "-t=0Q1w2E3r4T5y6" "-r=529b746d-ccd2-45fd-970b-f364e84b45a4:10,185bf54c-01a1-4806-a795-c84becd2e600:30"
```

### How to create a convenient batch file?
Create a .bat file with the following content (substituting your own parameters):
```
start "tmaa" /D c:\FOLDER\WHERE\UTILITY\IS\LOCATED tmaa.exe "-c=justinfan123" "-t=0Q1w2E3r4T5y6" "-r=529b746d-ccd2-45fd-970b-f364e84b45a4:10,185bf54c-01a1-4806-a795-c84becd2e600:30"
```
You can then assign an icon to the batch file and create a shortcut for convenient and neat usage.