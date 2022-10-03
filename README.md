# KK_MapUtils v1.0.0

## Introduction
Plugin for adjusting various map options set by map makers. Looks for gameobjects in map scenes with specific names. Any object named 'KK_MapUtils_DisableCharaLight' disables the character light when a scene is loaded. Point lights named 'KK_MapUtils_Day', 'KK_MapUtils_Evening', and 'KK_MapUtils_Night' will determine the ambient light color of the scene.

### Compatible with:
* Koikatu
* KoikatuVR
* Koikatsu Party
* Koikatsu Party VR

## Requirements
* [BepInEx 5.4.4](https://github.com/BepInEx/BepInEx/releases) or later

## Installation
Extract the `BepInEx` folder and paste it into your base `Koikatu` folder (it should merge with the `BepInEx` folder that's already there!)

## Usage (For Map Makers)
### Disable Chara Light
To mark a map for disabling the character light, simply create an object called "KK_MapUtils_DisableCharaLight" somewhere in your scene.<br/>
![](https://media.discordapp.net/attachments/696471324343926824/1026286540617429072/charalightdisable.jpg)

### Set Ambient Light
If your map has different groups of objects for day, evening, and night, simply place a point light in each group and call it 'KK_MapUtils_Day', 'KK_MapUtils_Evening', and 'KK_MapUtils_Night' respectively. (Be sure to disable the Light component!) The plugin will apply the color of the point light to the ambient light color of the map.

![](https://media.discordapp.net/attachments/696471324343926824/1026287012061388911/unknown.png)<br/>
![](https://media.discordapp.net/attachments/696471324343926824/1026287157381435444/unknown.png)