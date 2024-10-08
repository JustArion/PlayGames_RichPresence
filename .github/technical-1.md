### Technical Document #1

To read the info for the current game we need to read the `AppData/Local/Google/Play Games/Logs/Service.log`.<br>
The service log contains the necessary info to provide a rich-presence for Discord.
It contains:
- Startup Timestamp
- Package Name (For Icon / Art)
- Game Name

We can do some processing on our side to get an image via the `PackageName`<br>
To do this we simply do a `GET` request to `https://play.google.com/store/apps/details?id=<PackageName>` and then extract the `<meta property="og:image" content="PACKAGE_IMAGE_LINK">` tag from the head tag

We can differentiate the info from the log file via the `state` property in the log. There's 4 states that I know of but the format is a gRPC Message `.ToString()`-ed. If it were JSON it would have been easier.

States:
- Starting
- Running (Started)
- Stopping
- Stopped

States can include additional information like `state=Running={ "isVisible": true }` but for the sake of brevity, that information is not parsed.

---

**Below are some extracts from the relevant logs.**

Rich Presence:

```
240924 15:05:03.803+0 132 INFO  AppSessionModule: sessions updated: {
  is_multi_window=True
  app_session={
    session_id=9a2d8229-b485-4a30-99ad-c5e2d307635c
    package_name=com.YoStarEN.Arknights
    version_name=24.2.21
    version_code=1000103
    request={
      package_name=com.YoStarEN.Arknights
      preserve_if_already_running=False
      title=Arknights
      class_name=
      action=
      vm_window_mode=UnknownMode
      display_settings=
      mouse_input_mode=ModeTouchscreen
      version_name=24.2.21
      version_code=1000103
      aspect_ratio_limit={ }
      screen_orientation=SensorLandscape
      resolution_preference=
      architecture=4
      launch_source=ShortcutGeneric
    }
    window_mode=Windowed
    started_timestamp=9/24/2024 1:05:02 PM +00:00
    title=Arknights
    state=Running={ "isVisible": true }
    surface_state={
      foreground_task=com.YoStarEN.Arknights
      pcm_card_index=2
      guest_display={ id=4, overlay=Unknown }
      host_display={ id=16323076, visibility=Normal, mode=Windowed }
      host_display_density=235
      display_settings={ "displayDensity": 235, "displaySize": { "width": 1920, "height": 1080 }, "vsyncPeriodNs": 60 }
      mouse_input_mode=ModeTouchscreen
    }
  }
}
```

Exiting:

```
240924 15:17:26.457+0 86 INFO  AppSessionModule: sessions updated: {
  is_multi_window=True
  app_session={
    session_id=9a2d8229-b485-4a30-99ad-c5e2d307635c
    package_name=com.YoStarEN.Arknights
    version_name=24.2.21
    version_code=1000103
    request={
      package_name=com.YoStarEN.Arknights
      preserve_if_already_running=False
      title=Arknights
      class_name=
      action=
      vm_window_mode=UnknownMode
      display_settings=
      mouse_input_mode=ModeTouchscreen
      version_name=24.2.21
      version_code=1000103
      aspect_ratio_limit={ }
      screen_orientation=SensorLandscape
      resolution_preference=
      architecture=4
      launch_source=ShortcutGeneric
    }
    window_mode=Windowed
    started_timestamp=9/24/2024 1:05:02 PM +00:00
    title=Arknights
    state=Stopping={ "reason": "NORMAL_EXIT_USER_REQUESTED" }
    surface_state={
      foreground_task=com.YoStarEN.Arknights
      pcm_card_index=2
      guest_display={ id=4, overlay=Unknown }
      host_display={ id=16323076, visibility=Normal, mode=Windowed }
      host_display_density=235
      display_settings={ "displayDensity": 235, "displaySize": { "width": 1920, "height": 1080 }, "vsyncPeriodNs": 60 }
      mouse_input_mode=ModeTouchscreen
    }
  }
}
```
```
240924 15:17:26.457+0 60 INFO  AppSessionScope: game session ending for com.YoStarEN.Arknights with BssGameStopped, play time=00:12:23.7190789
```