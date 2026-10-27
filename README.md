<p align="center">
<img src="https://raw.githubusercontent.com/MaximumADHD/Roblox-API-Dump-Tool/master/Resources/AppLogo-White.png" width=80%>
</p>

<hr/>

# What is this?
The Roblox API Dump Tool is a utility program that allows you to browse a human-readable dump of Roblox's Lua API, and view upcoming changes to the API before they are officially released. I developed this tool alongside Roblox's JSON API Dump feature while I was an intern at Roblox.

This project's source code aims to be a foundational reference for working with the JSON API Dump, and how you can interpret its data for your own projects!
It also serves to replace Roblox's original API Dump, as this tool can generate a full dump of Roblox's API in a similar fashion to the original one, but with much more flexibility over how the data is presented.

# Download Link
You can download the latest version of the program directly through this link:
https://github.com/MaximumADHD/Roblox-API-Dump-Tool/releases

# Demonstrations

## API Dump
This tool can generate a full dump of Roblox's API based on the JSON API Dump!<br/>
You can view the full one [here](https://github.com/MaximumADHD/Roblox-Client-Tracker/blob/roblox/API-Dump.txt)!

Here's an example of what it generates for the AnimationTrack class:

```
Class AnimationTrack : Instance [NotCreatable]
    Property AnimationTrack.Animation: Animation? [ReadOnly] {🚧Animation}
    Property AnimationTrack.IsPlaying: boolean [ReadOnly] {🚧Animation} {🚀SimAccess}
    Property AnimationTrack.Length: number [ReadOnly] {🚧Animation}
    Property AnimationTrack.Looped: boolean {🚧Animation} {🚀SimAccess}
    Property AnimationTrack.Priority: Enum.AnimationPriority {🚧Animation} {🚀SimAccess}
    Property AnimationTrack.Speed: number [ReadOnly] {🚧Animation} {🚀SimAccess}
    Property AnimationTrack.TimePosition: number [NotReplicated] {🚧Animation} [📁LoadOnly] {🚀SimAccess}
    Property AnimationTrack.WeightCurrent: number [ReadOnly] {🚧Animation} {🚀SimAccess}
    Property AnimationTrack.WeightTarget: number [ReadOnly] {🚧Animation} {🚀SimAccess}
    Function AnimationTrack:AdjustSpeed(speed: number? = 1) -> () [CustomLuaState] {🚧Animation} {🚀SimAccess}
    Function AnimationTrack:AdjustWeight(weight: number? = 1, fadeTime: number? = 0.100000001) -> () [CustomLuaState] {🚧Animation} {🚀SimAccess}
    Function AnimationTrack:GetMarkerReachedSignal(name: string) -> RBXScriptSignal {🚧Animation}
    Function AnimationTrack:GetParameter(key: string) -> any {🚧Animation} {🚀SimAccess}
    Function AnimationTrack:GetParameterDefaults() -> { [string]: any } {🚧Animation}
    Function AnimationTrack:GetTargetInstance(name: string) -> Instance? {🚧Animation}
    Function AnimationTrack:GetTargetNames() -> { any } {🚧Animation}
    Function AnimationTrack:GetTimeOfKeyframe(keyframeName: string) -> number {🚧Animation}
    Function AnimationTrack:Play(fadeTime: number? = 0.100000001, weight: number? = 1, speed: number? = 1) -> () [CustomLuaState] {🚧Animation} {🚀SimAccess}
    Function AnimationTrack:SetParameter(key: string, value: any) -> () {🚧Animation} {🚀SimAccess}
    Function AnimationTrack:SetTargetInstance(name: string, target: Instance) -> () {🚧Animation}
    Function AnimationTrack:Stop(fadeTime: number? = 0.100000001) -> () [CustomLuaState] {🚧Animation} {🚀SimAccess}
    Function AnimationTrack:GetDebugData() -> { [string]: any } {🚧Animation} {🔒RobloxScript}
    Function AnimationTrack:ResetGraph() -> () {🚧Animation} {🔒RobloxScript}
    Function AnimationTrack:UpdateGraphNodeDefinition(nodeId: string, nodeDefinition: Instance) -> boolean {🚧Animation} {🔒RobloxScript}
    Function AnimationTrack:UpdateGraphNodeProperty(nodeId: string, propertyName: string, value: any, inputPinName: string? = "") -> boolean {🚧Animation} {🔒RobloxScript}
    Event AnimationTrack.DidLoop() {🚧Animation}
    Event AnimationTrack.Ended() {🚧Animation}
    Event AnimationTrack.KeyframeReached(keyframeName: string) {🚧Animation}
    Event AnimationTrack.Stopped() {🚧Animation}
    Event AnimationTrack.ParameterChanged(key: string, value: any) {🚧Animation} {🔒RobloxScript}
```

## API Changes

This tool is also capable of comparing API Dumps, and it can generate very precise difference logs.<br/>
Here are the API changes that were generated from this tool for Roblox v721:

```
Added Class ScriptDebuggerService : Instance [🏷️NotCreatable] [🏷️NotReplicated] [🏷️Service]
    Added Function ScriptDebuggerService:AddBreakpoint(scriptInstance: LuaSourceContainer, breakpoint: { [any]: any }) -> { [any]: any } {🔒Plugin}
    Added Function ScriptDebuggerService:ClearBreakpoints() -> () {🔒Plugin}
    Added Function ScriptDebuggerService:Evaluate(expression: string, frameId: number?) -> { [any]: any } {🔒Plugin}
    Added Function ScriptDebuggerService:GetRootVariables(frameId: number) -> { any } {🔒Plugin}
    Added Function ScriptDebuggerService:GetStackTrace(threadId: number, startFrame: number?) -> { [any]: any } {🔒Plugin}
    Added Function ScriptDebuggerService:GetThreads() -> { any } {🔒Plugin}
    Added Function ScriptDebuggerService:GetVariables(variablesReference: number) -> { any } {🔒Plugin}
    Added Function ScriptDebuggerService:Pause() -> () {🔒Plugin}
    Added Function ScriptDebuggerService:RemoveBreakpoint(scriptInstance: LuaSourceContainer, line: number) -> boolean {🔒Plugin}
    Added Function ScriptDebuggerService:SetExceptionBreakMode(breakMode: Enum.DebugBreakModeType) -> () {🔒Plugin}
    Added Event ScriptDebuggerService.Resumed(threadIds: { any }) {🔒Plugin}
    Added Callback ScriptDebuggerService.OnStopped(stopped: { [any]: any }) -> { [any]: any } {🔒Plugin}

Added Property Studio.PreferredTextSize: Enum.PreferredTextSize
Added Property VideoFrame.MaximumResolution: Enum.VideoSampleSize {🚧Audio + UI}
Added Property VideoPlayer.MaximumResolution: Enum.VideoSampleSize {🚧Audio + UI}
Added Property AudioPlayer.AssetRepresentation: Enum.AssetRepresentation {🚧Audio} {🔒RobloxScript} [🏷️Hidden] [🏷️NotReplicated]
Added Property Sound.AssetRepresentation: Enum.AssetRepresentation {🚧LegacySound} {🔒RobloxScript} [🏷️Hidden] [🏷️NotReplicated]
Added Property Studio.CameraMouseMultiplier: number {🔒RobloxScript}
Added Property Terrain.ValidationAssetContentMap: AssetContentMap {🚧Environment} {🔒RobloxEngine} [🏷️Hidden] [🏷️NotReplicated] [🏷️NotScriptable]
Added Property UserGameSettings.StudioPreferredTextSize: Enum.PreferredTextSize {🔒RobloxScript} [🏷️Hidden] [🏷️NotReplicated]
Added Property Workspace.WatermarkHash: string {🔒RobloxEngine} [🏷️Hidden] [🏷️NotScriptable]

Added Function GenerationService:ConnectAsync(sessionId: string, sdp: string, type: string, relay: string) -> ...any {🚧DynamicGeneration + InternalTest} [🏷️Yields]
Added Function GenerationService:DisconnectAsync(sessionId: string) -> boolean {🚧DynamicGeneration + InternalTest} [🏷️Yields]
Added Function GenerationService:GetVideoGenSessionAsync() -> ...any {🚧DynamicGeneration + InternalTest} [🏷️Yields]
Added Function GenerationService:GetVideoGenTriggersAsync(sessionId: string, lookbackSeconds: number) -> { [any]: any } {🚧DynamicGeneration + InternalTest} [🏷️Yields]
Added Function GenerationService:StartVideoGenSessionAsync(sessionId: string, prompt: string, imageData: string, imageS3Reference: string, triggers: { [any]: any }) -> boolean {🚧DynamicGeneration + InternalTest} [🏷️Yields]
Added Function GenerationService:UpdateVideoGenSessionPromptAsync(sessionId: string, prompt: string, imageData: string, imageS3Reference: string, mode: string) -> boolean {🚧DynamicGeneration + InternalTest} [🏷️Yields]
Added Function GenerationService:UpdateVideoGenSessionTriggersAsync(sessionId: string, triggers: { [any]: any }) -> boolean {🚧DynamicGeneration + InternalTest} [🏷️Yields]
Added Function Player:ClearCachedAvatarAppearance() -> () {🚧AvatarAppearance + Players}
Added Function Plugin:FinishFullLoading() -> () {🔒RobloxScript}
Added Function TestService:CaptureScreenshotAsync(artifactName: string? = "", options: { [any]: any }?) -> ...any {🚧InternalTest} [🏷️Yields]
Added Function AssetQualityService:FetchAssetQualitySummaryFromJobIdAsync(jobId: string, desiredQualityChecks: { any }) -> { [any]: any } {🔒RobloxScript} [🏷️Yields]

Added Enum AssetRepresentation
    Added EnumItem AssetRepresentation.FullLength : 0
    Added EnumItem AssetRepresentation.ShortPreview : 1

Added Enum DebugBreakModeType
    Added EnumItem DebugBreakModeType.Never : 0
    Added EnumItem DebugBreakModeType.Always : 1
    Added EnumItem DebugBreakModeType.Unhandled : 2

Added Enum DebuggerResumeType
    Added EnumItem DebuggerResumeType.StepInto : 0
    Added EnumItem DebuggerResumeType.StepOut : 1
    Added EnumItem DebuggerResumeType.StepOver : 2
    Added EnumItem DebuggerResumeType.Resume : 3

Added Enum ScriptStoppedReason
    Added EnumItem ScriptStoppedReason.Breakpoint : 0
    Added EnumItem ScriptStoppedReason.Exception : 1
    Added EnumItem ScriptStoppedReason.Pause : 2
    Added EnumItem ScriptStoppedReason.Step : 3
    Added EnumItem ScriptStoppedReason.Entry : 4

Added Enum ScriptVariableScope
    Added EnumItem ScriptVariableScope.Local : 0
    Added EnumItem ScriptVariableScope.Upvalue : 1
    Added EnumItem ScriptVariableScope.Global : 2

Added EnumItem CollisionFidelity.Scalable : 4
Added EnumItem ConnectionError.PlacelaunchVipOwnerNotPresent : 541
Added EnumItem ImageCombineType.NormalMapBlend : 6

Added Tag [🏷️NotReplicated] to Property UICorner.CornerRadius

Changed the read permissions, serialization and category of Property UICorner.BottomLeftRadius
    from: {🔍RobloxScript} [🚫None] "Data"
      to: {🔒None} [💾|📁Serialized] "Individual Corners"

Changed the read permissions, serialization and category of Property UICorner.BottomRightRadius
    from: {🔍RobloxScript} [🚫None] "Data"
      to: {🔒None} [💾|📁Serialized] "Individual Corners"

Changed the read permissions, serialization and category of Property UICorner.TopLeftRadius
    from: {🔍RobloxScript} [🚫None] "Data"
      to: {🔒None} [💾|📁Serialized] "Individual Corners"

Changed the read permissions, serialization and category of Property UICorner.TopRightRadius
    from: {🔍RobloxScript} [🚫None] "Data"
      to: {🔒None} [💾|📁Serialized] "Individual Corners"

Changed the security of Property Studio.Camera Speed Adjust Binding
    from: {🔒None}
      to: {🔒RobloxScript}

Changed the serialization of Property UICorner.CornerRadius
    from: [💾|📁Serialized]
      to: [📁LoadOnly]

Changed the capabilities and security of Function SoundService:SetInputDevice
    from: {🚧Audio} {🔒RobloxScript}
      to: {🚧Audio + InternalTest} {🔒None}

Changed the parameters of Function MarketplaceService:PerformPurchase
    from: (infoType: Enum.InfoType, productId: number, expectedPrice: number, requestId: string, isRobloxPurchase: boolean, collectibleItemId: string? = "", collectibleProductId: string? = "", idempotencyKey: string? = "", purchaseAuthToken: string? = "", timedOptionsDays: number? = 0)
      to: (infoType: Enum.InfoType, productId: number, expectedPrice: number, requestId: string, isRobloxPurchase: boolean, collectibleItemId: string? = "", collectibleProductId: string? = "", idempotencyKey: string? = "", purchaseAuthToken: string? = "", timedOptionsDays: number? = 0, purchasePayload: string? = "")

Changed the parameters of Event TextChatService.OnUserChatSettingUpdateServer
    from: ()
      to: (featureName: string)

Removed Event AvatarCreationService.ReplicateAvatarGenerationImageId

Removed Tags [🏷️Hidden] [🏷️NotReplicated] from Property UICorner.BottomLeftRadius
Removed Tags [🏷️Hidden] [🏷️NotReplicated] from Property UICorner.BottomRightRadius
Removed Tags [🏷️Hidden] [🏷️NotReplicated] from Property UICorner.TopLeftRadius
Removed Tags [🏷️Hidden] [🏷️NotReplicated] from Property UICorner.TopRightRadius
```
