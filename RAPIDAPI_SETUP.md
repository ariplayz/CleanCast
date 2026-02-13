# CleanCast - RapidAPI Setup Guide

## Overview
CleanCast now uses the RapidAPI youtube-to-mp4 service to download YouTube videos. This bypasses YouTube's throttling and 403 errors.

## Setup Steps

### 1. Get a RapidAPI Account
- Go to https://rapidapi.com/
- Sign up for a free account
- Verify your email

### 2. Subscribe to youtube-to-mp4 API
- Go to https://rapidapi.com/ytjar/api/youtube-to-mp4
- Click "Subscribe to Test" (free tier available)
- You'll get an API key

### 3. Configure CleanCast
Edit the settings file at:
```
%AppData%\CleanCast\settings.json
```

Add or update these fields:
```json
{
  "useHardwareDecoding": true,
  "rapidApiKey": "YOUR_API_KEY_HERE",
  "rapidApiHost": "youtube-to-mp4.p.rapidapi.com",
  "resolverApiTemplate": ""
}
```

Replace `YOUR_API_KEY_HERE` with your RapidAPI key.

### 4. Test It
- Run CleanCast
- Paste a YouTube URL and click "Add YouTube"
- Watch the download progress in the status area
- Once cached, it will show "Ready: VideoTitle"
- Click "Play Next" for instant playback from cache

## How It Works

### When You Add a YouTube Video:
1. App fetches video title/metadata
2. Adds to queue immediately
3. **Background download starts** → shows progress "Downloading VideoTitle: 45%"
4. Video cached to: `C:\Users\[YourUser]\AppData\Local\Temp\CleanCast\Cache\`

### When You Play:
1. Checks if cached file exists
2. **Plays instantly** from cache
3. No network delay, no throttling errors

### Cache Benefits:
- Same video never downloaded twice (saved by video ID)
- Instant playback after first download
- Works offline once cached
- Cache is in Windows Temp folder (auto-cleaned periodically)

## Troubleshooting

### "API key not configured"
- Check that you added `rapidApiKey` to settings.json
- Make sure the key is correct (copy from RapidAPI dashboard)
- Restart the app after updating settings.json

### "API error 403"
- Check your RapidAPI plan limits (free tier has limits)
- Verify API key is correct
- Check RapidAPI dashboard for subscription status

### "Failed to download stream"
- Check your internet connection
- Verify the YouTube URL is valid
- Check if the video is age-restricted or region-locked
- Try the video on youtube.com to verify it works

## API Limits

RapidAPI youtube-to-mp4 free tier typically allows:
- 100 requests per day
- Videos up to ~30 minutes

If you need more, upgrade to a paid plan on RapidAPI.

## Privacy Note

- Only your RapidAPI key is sent to RapidAPI
- YouTube URLs are sent only to fetch stream metadata
- Downloaded videos are stored locally only
- No data collection from CleanCast

