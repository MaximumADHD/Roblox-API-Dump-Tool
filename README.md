<p align="center">
<img src="https://raw.githubusercontent.com/CloneTrooper1019/Roblox-API-Dump-Tool/master/Resources/AppLogo.png" width=80%>
</p>

<hr/>

# What is this?
The Roblox API Dump Tool is a utility program that allows you to browse a human-readable dump of Roblox's Lua API, and view upcoming changes to the API before they are officially released. I developed this tool alongside Roblox's JSON API Dump feature while I was an intern at Roblox.

This project's source code aims to be a foundational reference for working with the JSON API Dump, and how you can interpret its data for your own projects!
It also serves to replace Roblox's original API Dump, as this tool can generate a full dump of Roblox's API in a similar fashion to the original one, but with much more flexibility over how the data is presented.

# Download Link
You can download the latest version of the program directly through this link:
https://raw.githubusercontent.com/CloneTrooper1019/Roblox-API-Dump-Tool/master/RobloxAPIDumpTool.exe

# Demonstrations

## API Dump
This tool can generate a full dump of Roblox's API based on the JSON API Dump!<br/>
The full dump is ~3000 lines though, so I won't embed it in this README.<br/>
You can view the full one [here](https://github.com/CloneTrooper1019/Roblox-Client-Watch/blob/roblox/API-Dump.txt)!

Here's an example of what it generates for the AnimationTrack class:

```
Class AnimationTrack : Instance [NotCreatable]
    Property Class<Animation> AnimationTrack.Animation [ReadOnly] [NotReplicated]
    Property bool AnimationTrack.IsPlaying [ReadOnly] [NotReplicated]
    Property float AnimationTrack.Length [ReadOnly] [NotReplicated]
    Property bool AnimationTrack.Looped
    Property Enum<AnimationPriority> AnimationTrack.Priority
    Property float AnimationTrack.Speed [ReadOnly] [NotReplicated]
    Property float AnimationTrack.TimePosition [NotReplicated]
    Property float AnimationTrack.WeightCurrent [ReadOnly] [NotReplicated]
    Property float AnimationTrack.WeightTarget [ReadOnly] [NotReplicated]
    Function void AnimationTrack:AdjustSpeed(float speed = 1)
    Function void AnimationTrack:AdjustWeight(float weight = 1, float fadeTime = 0.100000001)
    Function RBXScriptSignal AnimationTrack:GetMarkerReachedSignal(string name)
    Function double AnimationTrack:GetTimeOfKeyframe(string keyframeName)
    Function void AnimationTrack:Play(float fadeTime = 0.100000001, float weight = 1, float speed = 1)
    Function void AnimationTrack:Stop(float fadeTime = 0.100000001)
    Event AnimationTrack.DidLoop()
    Event AnimationTrack.KeyframeReached(string keyframeName)
    Event AnimationTrack.Stopped()
```

## API Changes

This tool is also capable of comparing API Dumps, and it can generate very precise difference logs.<br/>
Here are the API changes that were generated from this tool for Roblox v366:

```
Added Class KeyframeMarker : Instance
    Added Property string KeyframeMarker.Value

Added Class LocalStorageService : Instance [NotCreatable] [Service] [NotReplicated]
    Added Function void LocalStorageService:Flush() {RobloxScriptSecurity}
    Added Function string LocalStorageService:GetItem(string key) {RobloxScriptSecurity}
    Added Function void LocalStorageService:SetItem(string key, string value) {RobloxScriptSecurity}
    Added Function void LocalStorageService:WhenLoaded(Function callback) {RobloxScriptSecurity}
    Added Event LocalStorageService.ItemWasSet(string key, string value) {RobloxScriptSecurity}
    Added Event LocalStorageService.StoreWasCleared() {RobloxScriptSecurity}

Added Class AppStorageService : LocalStorageService [NotCreatable] [Service] [NotReplicated]
Added Class UserStorageService : LocalStorageService [NotCreatable] [Service] [NotReplicated]

Added Property Enum<DEPRECATED_DebuggerDataModelPreference> Studio.Attach Debugger To
Added Property bool Studio.LuaDebuggerEnabledAtStartup [Hidden] [ReadOnly]
Added Property bool ReflectionMetadataItem.ClientOnly
Added Property bool ReflectionMetadataItem.ServerOnly

Added Function RBXScriptSignal AnimationTrack:GetMarkerReachedSignal(string name)
Added Function Objects BasePlayerGui:GetGuiObjectsAtPosition(int x, int y)
Added Function void Humanoid:ApplyDescription(Instance humanoidDescription) [Yields]
Added Function void Humanoid:CacheDefaults() {RobloxScriptSecurity}
Added Function Instance Humanoid:GetAppliedDescription()
Added Function void Keyframe:AddMarker(Instance marker)
Added Function Objects Keyframe:GetMarkers()
Added Function void Keyframe:RemoveMarker(Instance marker)
Added Function void StarterPlayer:ClearDefaults() {RobloxScriptSecurity}

Added Enum DEPRECATED_DebuggerDataModelPreference
    Added EnumItem DEPRECATED_DebuggerDataModelPreference.Server : 0
    Added EnumItem DEPRECATED_DebuggerDataModelPreference.Client : 1

Changed the parameters and security of Function Plugin:StartDrag 
    from: (PluginDrag drag) {RobloxScriptSecurity}
      to: (Dictionary dragData) {PluginSecurity}

Changed the parameters and security of Event PluginGui.PluginDragDropped 
    from: (Instance pluginDragEvent) {RobloxScriptSecurity}
      to: (Dictionary dragData) {PluginSecurity}

Changed the parameters and security of Event PluginGui.PluginDragEntered 
    from: (Instance pluginDragEvent) {RobloxScriptSecurity}
      to: (Dictionary dragData) {PluginSecurity}

Changed the parameters and security of Event PluginGui.PluginDragLeft 
    from: (Instance pluginDragEvent) {RobloxScriptSecurity}
      to: (Dictionary dragData) {PluginSecurity}

Changed the parameters and security of Event PluginGui.PluginDragMoved 
    from: (Instance pluginDragEvent) {RobloxScriptSecurity}
      to: (Dictionary dragData) {PluginSecurity}

Removed Property Studio.Debug Client In APS Mode
```