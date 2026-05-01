# Tebex for Unity Engine

Tebex is the easiest platform for adding payments to your Unity game. Connect a store and add payments
with our no-code solution for both desktop and mobile.

## Features
- Included store browser, add your own branding via editor or code
- Built-in cart system with support for creator codes and discount codes
- In-app checkout for both Native and WebGL
- Show a QR code to customer for checkout
- Complete Headless API SDK allows you to quickly integrate into your game's own UI
- Deliverables system allows you to apply game actions when payments are completed

## Getting Started

### 1. Deliverables
Use Deliverables to run functions when a payment is completed on a basket. You can provide a custom action to run when a certain package ID is purchased.

**Registering Deliverables**
```csharp
Deliverables.RegisterDeliverableAction(12345, package =>
{
    // get player, add coins, unlock item, etc.
});
```

**Applying Deliverables**

Set `Deliverables.ActiveBasket` to the customer's basket prior to checkout to apply in-game actions on the basket when checkout is completed.

It will check periodically `CheckActiveBasket()` for the basket to be completed or expired once tracked.
If using the **StoreBrowser**, the customer's active basket is automatically tracked.

TODO: To apply deliverables that have already been purchased (such as when restarting the game), use `Deliverables.ApplyPurchases()`

### 2. StoreBrowser
The StoreBrowser is a drop-in MonoBehavior that draws a UI for your Tebex store in-game.

To use the StoreBrowser, you'll define 2 panels on which the store elements will be rendered:

| Panel | Description |
|-------|-------------|
| CategoriesPanel | The panel where the store's category selectors will be rendered |
| ProductPanel | The panel where the products will be rendered |

Then, set the `StorePublicKey` to the API key provided in your Tebex panel. 
Set the position and size of each panel as per your game's requirements.

**Opening the Store Browser**

To open the store browser, run `Tebex.StoreBrowser.Open()`, which will fetch the store and draw it on the indicated panels.

**Customization Options**

Below are the options available for the StoreBrowser's customization and appearance:

| Option               | Description                                              |
|----------------------|----------------------------------------------------------|
| StorePublicKey       | The Public Key of your game's store, used for API access |
| StoreBgColor         | The background color of the store UI                     |
| StoreBgImage         | The background image of the store UI                     |
| StoreLogo            | Store's logo, if any                     |
| StoreTextColor       | Normal text color to use                                 |
| StoreTextFont        | Font used for store text                                 |
| StoreHeaderTextSize  | Size of the header text                                  
| StoreTextSize        | Size of normal text                                      |
| StoreTextShadowSize  | Size of the text shadow, if any                          
| StoreTextShadowColor | Color of the store text shadow                           |
| StorePackageSizePx   | Size of the store's product container, in pixels         |
| StorePackagesPerRow  | Number of packages to show in each row                   |

### 3. InGameCart
This is a utility class that allows you to easily create and carry a remote cart for the player from the client-side, as well as display the cart in-game.

The InGameCart requires you to define a panel where we will render the cart:
- CartPanel

There are several options available for the cart's configuration:

| Option               | Description                                                                                           |
|----------------------|-------------------------------------------------------------------------------------------------------|
| EnableQR             | Shows a QR code at checkout                                                                           |
| EnableLinkout*       | Linkout checkout, opens checkout in the customer's browser (check device/app store restrictions) |
| EnableInAppCheckout* | In-app checkout, opens checkout in the game (check device/app store restrictions)                     |
| EnableTebexLogo      | Shows the Tebex logo on the cart page if enabled                                                      |
| EnableCreatorCode    | Allow the user to enter a creator code                                                                |
| EnableDiscountCode   | Allow the user to enter a discount code                                                               |
| CartPanel            | The panel in which the cart will be drawn                                                             |
| CartLogo             | The logo to show in the cart panel                                                                    |
| CheckoutBtnColorBg   | The background color of the checkout button                                                           |
| CheckoutBtnColorFg   | The foreground color of the checkout button                                                           |
* only one of these options can be enabled at a time

If enabled, the InGameCart can show a QR code allowing the customer to checkout using an external device.

### 4. Checkout

#### QR Codes
You may show a QR code to the customer for checkout. Codes are generated client-side for a specific URL.

### Code Examples

You are also free to build your own integration per your game's requirements. Mappings for the Tebex Headless API makes this easy. 

For in-depth code examples and documentation, see our GitBook: https://docs.tebex.io/developers/unity-engine/examples