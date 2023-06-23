# Audio-Alchemist

<img src="/Assets/Editor/AudioAlchemist/HeaderImage.png" alt="Audio Alchemist Icon">

The Audio Alchemist tool is a versatile and flexible solution for managing sounds in Unity projects. It simplifies the process of controlling sound effects from multiple scripts and provides options for fading effects and volume control.

<img src="/Assets/Editor/Docs/Audio%20alchemist%20Overview.png" alt="Overview">

## Features

Trigger sounds with a single line of code.
Customize sound properties:

<ul>
  <li>Control sound volume</li>
  <li>Looping</li>
  <li>Pitch adjustment</li>
  <li>Fade In/Out effect</li>
</ul>

## Usage

To trigger a sound, use the following code:

<img src="/Assets/Editor/Docs/Skin%20Unlock%20Sound.png" alt="Overview sound">

<p>To play a sound</p>

```csharp
AudioAlchemist.Instance.PlaySound("Skin Unlocked");
```

<p>To stop a sound</p>

```csharp
AudioAlchemist.Instance.StopSound("Skin Unlocked");
```

You can also control a whole group of sounds or sound by using a slider/buttons.

Example:

<ul>
  <li>In the Inspector, provide the name of the sound group you want to control in the "Group Name" field.</li>
  <li>Create a slider using the Canvas component</li>
  <img src="/Assets/Editor/Docs/GUI%20Slider.png" alt="GUI Slider Component">
  <li>Reference a slider from any script, drag and drop the script on "On Value Changed" where the following function is at.</li>
  <img src="/Assets/Editor/Docs/GUI%20Sliders%20Controller.png" alt="GUI Slider On Value Change">
  <li>When the slider value changes, the "OnVolumeSliderChanged" method will be invoked, which in turn calls the "UpdateSubjectVolume" method of the "AudioAlchemist" instance, passing the sound group name and the new volume value.</li>
</ul>

```csharp
public void GUISoundsController(string groupName)
    {
        musicSlider.onValueChanged.AddListener((v) =>
        {
            AudioAlchemist.Instance.UpdateSubjectVolume(groupName, v);
        });
    }
```
