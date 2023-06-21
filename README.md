# Audio-Alchemist

<img src="/Assets/Editor/AudioAlchemist/HeaderImage.png" alt="Audio Alchemist Icon">

The Audio Alchemist tool is a versatile and flexible solution for managing sounds in Unity projects. It simplifies the process of controlling sound effects from multiple scripts and provides options for fading effects and volume control.

<img src="/Assets/Docs/Audio%20Alchemist%20Overview.png" alt="Overview">

## Features

- Trigger sounds with a single line of code.
- Customize sound properties
<ul>
  <li>Control sound volume</li>
  <li>Looping</li>
  <li>Pitch adjustment</li>
  <li>Fade In/Out effect</li>
</ul>

## Usage

To trigger a sound, use the following code:

<img src="/Assets/Docs/Skin%20Unlock%20Sound.png" alt="Overview sound">

To play a sound

```csharp
AudioAlchemist.Instance.PlaySound("Skin Unlocked");
```

To stop a sound

```csharp
AudioAlchemist.Instance.StopSound("Skin Unlocked");
```

You can also control a whole group of sounds
