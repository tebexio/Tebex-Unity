![Tebex Logo](https://www.tebex.io/_nuxt/logo.BCN2mLkL.svg)
# Tebex Plugin for Unity Engine

This is the official [Tebex](https://tebex.io/) plugin for Unity. It implements our [Tebex C# SDK](https://github.com/tebexio/Tebex-CSharp), which is designed to be forked and modified to your particular requirements.

Tebex is a gaming-focused payments processor that allows you to easily set up your game for monetization.

## Installation and Usage

1. Create a `Plugins` folder in your Unity project's assets.
2. Place a compiled `Tebex-CSharp.dll` into your `Plugins` folder: [Tebex C# SDK Releases](https://github.com/tebexio/Tebex-CSharp/releases)
3. Place the `UnityTebexPlugin.cs` in your Unity project's assets, such as in a `Scripts/` folder
4. Create an empty GameObject to contain the Tebex plugin.
5. Attach `UnityTebexPlugin` as a component to the new game object.
6. Start the game and monitor console for details. Tebex should connect and pull information about the connected store.

## Features

## What is Tebex?
[Tebex](https://tebex.io) allows game studios and/or players to set up a digital storefront and sell **Packages**. Your storefront may be a website or fully integrated into your game using **Headless API**.

Originally known as Buycraft, Tebex supports an expanding library of games, including:
- Minecraft
- Rust
- ARK: Survival Evolved
- FiveM
- Garry's Mod
- Unturned

### Tebex Unity Engine SDK
This repository serves as our full Tebex SDK for the Unity Engine, allowing for quick and seamless integration into your game. It is designed to be forked and modified for your particular engine version and your game's requirements.

The example plugin here was built for `Unity 6000.0.47f1`.

## Integration Guide

A minimal Tebex integration is expected to:
1. Connect to a store via a secret key [accessible from your Tebex account](https://tebex.io/)
2. Add a player's desired packages to a basket, or direct the player to the relevant purchase area 
3. Direct the user to check out (performed in the browser)
4. Deliver purchases to the player after payment in some way

Most typically, purchases are delivered via **Game Commands** (such as adding money, adding items, etc.) that are queued and executed every few minutes. This is implemented in TebexCorePlugin from our Tebex-CSharp SDK.

For more advanced delivery methods, Tebex also provides [Webhooks](https://docs.tebex.io/developers/webhooks/overview) to allow fully custom handling of purchases on your own backend.

## Contributions
We welcome contributions from the community. Please refer to the `CONTRIBUTING.md` file for more details. By submitting code to us, you agree to the terms set out in the `CONTRIBUTING.md` file

## Support
This repository is only used for bug reports via GitHub Issues. If you have found a bug, please [open an issue](https://github.com/tebexio/Tebex-Unity/issues).

If you are a user requiring support for Tebex, please contact us at https://tebex.io/contact