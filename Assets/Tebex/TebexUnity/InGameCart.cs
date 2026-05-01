using System;
using System.Collections;
using System.Globalization;
using Tebex.Headless;
using Tebex.QR;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Tebex.TebexUnity
{
    public class InGameCart : MonoBehaviour
    {
        [SerializeField] public string StorePublicKey;
        [SerializeField] public Canvas CartCanvas;
        [SerializeField] public bool EnableQR;
        [SerializeField] public bool EnableTebexLogo;
        [SerializeField] public bool EnableCreatorCode;
        [SerializeField] public bool EnableDiscountCode;
        [SerializeField] public Texture2D CartLogo;

        [SerializeField] public Color32 CheckoutBtnColorBg = Color.cadetBlue;
        [SerializeField] public Color32 CheckoutBtnColorFg = Color.white;
        [SerializeField] public TMP_FontAsset Font;

        private RectTransform cartRect;
        private bool isOpen;
        private Coroutine animRoutine;
        private float _cartWidth = 350f;
        private float _scale = 1f;

        private bool _needsUpdate;

        public Basket ActiveBasket { get; private set; }

        private void Update()
        {
            if (_needsUpdate)
            {
                _needsUpdate = false;
                DrawUI();
            }
        }

        #region Async API

        public void NewBasketWithUsername(string username)
        {
            HeadlessApi.GetInstance().CreateBasketAsync(
                new CreateBasketPayload("", username),
                res =>
                {
                    ActiveBasket = res.data;
                    _needsUpdate = true;
                },
                err => Debug.LogError(err.title),
                srv => Debug.LogError(srv.Body)
            );
        }

        public void NewBasketWithEmail(string email)
        {
            HeadlessApi.GetInstance().CreateBasketAsync(
                new CreateBasketPayload(email, ""),
                res =>
                {
                    ActiveBasket = res.data;
                    _needsUpdate = true;
                },
                err => Debug.LogError(err.title),
                srv => Debug.LogError(srv.Body)
            );
        }

        public void AddPackage(Package package)
        {
            if (ActiveBasket == null) return;

            HeadlessApi.GetInstance().AddPackageToBasketAsync(
                ActiveBasket.ident,
                new AddPackagePayload(package.id),
                res =>
                {
                    ActiveBasket = res.data;
                    _needsUpdate = true;
                },
                err => Debug.LogError(err.title),
                srv => Debug.LogError(srv.Body)
            );
        }

        public void RemovePackage(BasketPackage package)
        {
            if (ActiveBasket == null) return;

            HeadlessApi.GetInstance().RemovePackageFromBasketAsync(
                ActiveBasket.ident,
                package.id,
                res =>
                {
                    ActiveBasket = res.data;
                    _needsUpdate = true;
                },
                err => Debug.LogError(err.title),
                srv => Debug.LogError(srv.Body)
            );
        }

        public void SetCreatorCode(string code)
        {
            if (ActiveBasket == null) return;

            HeadlessApi.GetInstance().ApplyCreatorCodeAsync(
                ActiveBasket.ident,
                code,
                res => { _needsUpdate = false; },
                err => Debug.LogError(err.title),
                srv => Debug.LogError(srv.Body)
            );
        }

        public void SetDiscountCode(string code)
        {
            if (ActiveBasket == null) return;

            HeadlessApi.GetInstance().ApplyCouponAsync(
                ActiveBasket.ident,
                code,
                res => { _needsUpdate = false; },
                err => Debug.LogError(err.title),
                srv => Debug.LogError(srv.Body)
            );
        }

        #endregion

        #region UI

        public void DrawUI()
        {
            if (CartCanvas == null || ActiveBasket == null) return;

            foreach (Transform child in CartCanvas.transform)
                Destroy(child.gameObject);

            var root = CreateUIObject("CartRoot", CartCanvas.transform);
            var rootRect = root.GetComponent<RectTransform>();

            // Compute cart width relative to canvas size.
            // On portrait / narrow screens (phones) use 50 % of width so the cart fills
            // roughly half the screen.  On landscape / desktop use 25 % with a 350 pt floor.
            var canvasRt = CartCanvas.GetComponent<RectTransform>();
            float canvasWidth  = canvasRt.rect.width  > 0 ? canvasRt.rect.width  : 1920f;
            float canvasHeight = canvasRt.rect.height > 0 ? canvasRt.rect.height : 1080f;

            bool isNarrow = canvasWidth < canvasHeight; // portrait / phone
            _cartWidth = isNarrow
                ? canvasWidth * 0.50f                    // ~50 % on phones
                : Mathf.Max(350f, canvasWidth * 0.25f);  // 25 % (min 350) on landscape / desktop

            // Scale fonts and padding against a 350-unit baseline; clamp so nothing
            // becomes unreadably small on narrow screens.
            _scale = Mathf.Max(0.6f, _cartWidth / 350f);

            // Anchored to the right edge, slides in/out horizontally.
            // Start off-screen so Open() animates it in.
            rootRect.anchorMin = new Vector2(1, 0);
            rootRect.anchorMax = new Vector2(1, 1);
            rootRect.pivot     = new Vector2(1, 0.5f);
            rootRect.offsetMin = new Vector2(-_cartWidth, 0);
            rootRect.offsetMax = new Vector2(0, 0);
            rootRect.anchoredPosition = new Vector2(_cartWidth, 0); // hidden off-screen to the right

            cartRect = rootRect;

            // If the cart was already open when DrawUI was called (e.g. from an API callback
            // after adding an item), keep it visible rather than resetting to off-screen.
            if (isOpen)
                rootRect.anchoredPosition = new Vector2(0, 0);

            root.AddComponent<Image>().color = new Color(0, 0, 0, 0.98f);

            int pad = Mathf.RoundToInt(10 * _scale);
            var layout = root.AddComponent<VerticalLayoutGroup>();
            layout.padding           = new RectOffset(pad, pad, pad, pad);
            layout.spacing           = 10 * _scale;
            layout.childControlHeight = true;
            layout.childControlWidth  = true;
            layout.childForceExpandHeight = false;

            // HEADER ROW: logo (optional) + close button
            var headerRow = CreateUIObject("HeaderRow", root.transform);
            var headerLayout = headerRow.AddComponent<HorizontalLayoutGroup>();
            headerLayout.childForceExpandWidth  = false;
            headerLayout.childForceExpandHeight = false;
            headerLayout.childControlWidth  = true;
            headerLayout.childControlHeight = true;
            headerLayout.spacing = 5 * _scale;
            headerRow.AddComponent<LayoutElement>().preferredHeight = 50 * _scale;

            // if (CartLogo != null)
            // {
            //     var logoObj = CreateUIObject("Logo", headerRow.transform);
            //     var logoImg = logoObj.AddComponent<RawImage>();
            //     logoImg.texture = CartLogo;
            //     var logoElem = logoObj.AddComponent<LayoutElement>();
            //     logoElem.preferredWidth = 120;
            //     logoElem.flexibleWidth  = 0;
            //     logoElem.preferredHeight = 80;
            // }

            // Spacer to push close button right
            var spacer = CreateUIObject("Spacer", headerRow.transform);
            spacer.AddComponent<LayoutElement>().flexibleWidth = 1;

            // Close button
            var closeBtn = CreateUIObject("CloseButton", headerRow.transform);
            closeBtn.AddComponent<Image>().color = new Color(0.15f, 0.15f, 0.15f, 1f);
            var closeBtnElem = closeBtn.AddComponent<LayoutElement>();
            closeBtnElem.preferredWidth  = 40 * _scale;
            closeBtnElem.preferredHeight = 40 * _scale;
            closeBtnElem.flexibleWidth   = 0;
            var closeBtnComp = closeBtn.AddComponent<Button>();
            closeBtnComp.onClick.AddListener(Close);

            var closeTxt = CreateUIObject("X", closeBtn.transform).AddComponent<TextMeshProUGUI>();
            closeTxt.text = "X";
            closeTxt.fontSize = 14 * _scale;
            closeTxt.alignment = TextAlignmentOptions.Center;
            closeTxt.color = Color.white;
            closeTxt.font = Font;
            var closeTxtRect = closeTxt.rectTransform;
            closeTxtRect.anchorMin = Vector2.zero;
            closeTxtRect.anchorMax = Vector2.one;
            closeTxtRect.offsetMin = Vector2.zero;
            closeTxtRect.offsetMax = Vector2.zero;

            // SCROLL VIEW (ITEMS)
            var scrollRoot = CreateUIObject("ScrollView", root.transform);
            var scroll = scrollRoot.AddComponent<ScrollRect>();
            scroll.vertical   = true;
            scroll.horizontal = false;
            scrollRoot.AddComponent<LayoutElement>().flexibleHeight = 1;

            var viewport = CreateUIObject("Viewport", scrollRoot.transform);
            viewport.AddComponent<Image>().color = Color.clear;
            // viewport.AddComponent<Mask>().showMaskGraphic = false;
            var viewportRect = viewport.GetComponent<RectTransform>();
            viewportRect.anchorMin = Vector2.zero;
            viewportRect.anchorMax = Vector2.one;
            viewportRect.offsetMin = Vector2.zero;
            viewportRect.offsetMax = Vector2.zero;

            var content = CreateUIObject("Content", viewport.transform);

            // Anchor content to the top of the viewport so items stack downward from y=top.
            // Width fills the viewport via anchors; height grows via ContentSizeFitter.
            var contentRect = content.GetComponent<RectTransform>();
            contentRect.anchorMin = new Vector2(0, 1);
            contentRect.anchorMax = new Vector2(1, 1);
            contentRect.pivot     = new Vector2(0.5f, 1f);
            contentRect.sizeDelta = Vector2.zero;

            var vlg = content.AddComponent<VerticalLayoutGroup>();
            vlg.spacing             = 5 * _scale;
            vlg.childControlHeight  = true;
            vlg.childControlWidth   = true;
            vlg.childForceExpandWidth = true;

            // Only grow height — width is handled by the anchor span above.
            var csf = content.AddComponent<ContentSizeFitter>();
            csf.verticalFit   = ContentSizeFitter.FitMode.PreferredSize;
            csf.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;

            scroll.viewport = viewportRect;
            scroll.content  = contentRect;

            // ITEMS
            Debug.Log($"Basket has {ActiveBasket.packages.Count} packages");
            foreach (var p in ActiveBasket.packages)
                CreateItemRow(content.transform, p);

            // INPUTS
            if (EnableCreatorCode || EnableDiscountCode)
            {
                var row = CreateGridRow(root.transform, 2);

                if (EnableCreatorCode)
                    CreateInputField(row.transform, "Creator Code", SetCreatorCode);

                if (EnableDiscountCode)
                    CreateInputField(row.transform, "Discount Code", SetDiscountCode);
            }

            // TOTAL
            var totalObj = CreateUIObject("Total", root.transform);
            totalObj.AddComponent<LayoutElement>().preferredHeight = 30 * _scale;
            var totalText = totalObj.AddComponent<TextMeshProUGUI>();
            totalText.text = $"Total: {ActiveBasket.total_price.ToString("F2", CultureInfo.InvariantCulture)} {ActiveBasket.currency}";
            totalText.alignment = TextAlignmentOptions.Center;
            totalText.fontSize = 16 * _scale;
            totalText.font = Font;
            totalText.color = Color.white;

            CreateCheckoutButton(root.transform);
        }

        private void CreateItemRow(Transform parent, BasketPackage p)
        {
            var row = CreateGridRow(parent, 3);
            row.GetComponent<GridLayoutGroup>().cellSize = new Vector2(100 * _scale, 80 * _scale);

            var packageImage = CreateUIObject("Icon" + p.name, row.transform).AddComponent<RawImage>();
            packageImage.texture = Tebex.GetTexture(Tebex.PackageLookup[p.id]);

            var text = CreateUIObject("Text" + p.name, row.transform).AddComponent<TextMeshProUGUI>();
            text.text = $"{p.in_basket.quantity}x {p.name}\n({(p.in_basket.price * p.in_basket.quantity).ToString("F2", CultureInfo.InvariantCulture)} {ActiveBasket.currency})";
            text.font  = Font;
            text.color = Color.white;
            text.fontSize = 16 * _scale;
            text.alignment = TextAlignmentOptions.Left;
            text.verticalAlignment = VerticalAlignmentOptions.Middle;

            var btnObj = CreateUIObject("Remove", row.transform);
            btnObj.AddComponent<LayoutElement>().preferredHeight = 50 * _scale;
            var btn = btnObj.AddComponent<Button>();
            btn.onClick.AddListener(() => RemovePackage(p));

            var xTxt = CreateUIObject("X", btnObj.transform).AddComponent<TextMeshProUGUI>();
            xTxt.text = "X";
            xTxt.fontSize = 24 * _scale;
            xTxt.alignment = TextAlignmentOptions.Center;
            xTxt.verticalAlignment = VerticalAlignmentOptions.Middle;
            xTxt.color = Color.white;
            var xRect = xTxt.rectTransform;
            xRect.anchorMin = Vector2.zero;
            xRect.anchorMax = Vector2.one;
            xRect.offsetMin = Vector2.zero;
            xRect.offsetMax = Vector2.zero;
        }

        private GameObject CreateGridRow(Transform parent, int cols)
        {
            var obj = CreateUIObject("Row", parent);
            var grid = obj.AddComponent<GridLayoutGroup>();
            grid.constraint      = GridLayoutGroup.Constraint.FixedColumnCount;
            grid.constraintCount = cols;
            grid.cellSize        = new Vector2(80 * _scale, 80 * _scale);
            obj.AddComponent<LayoutElement>().preferredHeight = 80 * _scale;
            return obj;
        }

        private void CreateCheckoutButton(Transform parent)
        {
            var btnObj = CreateUIObject("Checkout", parent);
            btnObj.AddComponent<LayoutElement>().preferredHeight = 50 * _scale;

            Color bgColor = CheckoutBtnColorBg;
            if (bgColor.a < 0.01f) bgColor = Color.orange;
            Color fgColor = CheckoutBtnColorFg;
            if (fgColor.a < 0.01f) fgColor = Color.white;

            btnObj.AddComponent<Image>().color = bgColor;
            var btn = btnObj.AddComponent<Button>();
            btn.onClick.AddListener(ShowCheckoutModal);

            var text = CreateUIObject("Text", btnObj.transform).AddComponent<TextMeshProUGUI>();
            text.text  = "Proceed To Checkout";
            text.alignment = TextAlignmentOptions.Center;
            text.verticalAlignment = VerticalAlignmentOptions.Middle;
            text.color = Color.black;
            text.fontSize = 16 * _scale;
            text.font = Font;

            var textRect = text.rectTransform;
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = Vector2.zero;
            textRect.offsetMax = Vector2.zero;
        }

        private void ShowCheckoutModal()
        {
            if (ActiveBasket == null || CartCanvas == null) return;
            if (string.IsNullOrEmpty(ActiveBasket.links?.checkout)) return;

            // Full-screen blocking overlay — rendered on top of the cart panel
            var overlay = CreateUIObject("CheckoutModal", CartCanvas.transform);
            var overlayRect = overlay.GetComponent<RectTransform>();
            overlayRect.anchorMin = Vector2.zero;
            overlayRect.anchorMax = Vector2.one;
            overlayRect.offsetMin = Vector2.zero;
            overlayRect.offsetMax = Vector2.zero;
            overlay.AddComponent<Image>().color = new Color(0f, 0f, 0f, 0.78f);

            // Centered panel
            float panelWidth  = Mathf.Max(380f, _cartWidth * 1.15f);
            float qrSize      = panelWidth * 0.65f;
            float panelHeight = 60 * _scale + 50 * _scale + qrSize + 60 * _scale + 60 * _scale; // title+body+qr+btn+padding

            var panel = CreateUIObject("Panel", overlay.transform);
            var panelRect = panel.GetComponent<RectTransform>();
            panelRect.anchorMin = new Vector2(0.5f, 0.5f);
            panelRect.anchorMax = new Vector2(0.5f, 0.5f);
            panelRect.pivot     = new Vector2(0.5f, 0.5f);
            panelRect.sizeDelta = new Vector2(panelWidth, panelHeight);

            panel.AddComponent<Image>().color = new Color(0.07f, 0.07f, 0.07f, 1f);

            int pad = Mathf.RoundToInt(20 * _scale);
            // Extra top padding leaves room for the overlaid X button
            var panelLayout = panel.AddComponent<VerticalLayoutGroup>();
            panelLayout.padding               = new RectOffset(pad, pad, Mathf.RoundToInt(50 * _scale), pad);
            panelLayout.spacing               = 10 * _scale;
            panelLayout.childAlignment        = TextAnchor.UpperCenter;
            panelLayout.childControlWidth     = true;
            panelLayout.childControlHeight    = true;
            panelLayout.childForceExpandWidth  = true;
            panelLayout.childForceExpandHeight = false;

            // --- X close button — overlaid at top-right, outside the VLG flow ---
            var closeBtn = CreateUIObject("CloseBtn", panel.transform);
            closeBtn.AddComponent<LayoutElement>().ignoreLayout = true;
            var closeBtnRect = closeBtn.GetComponent<RectTransform>();
            closeBtnRect.anchorMin        = new Vector2(1f, 1f);
            closeBtnRect.anchorMax        = new Vector2(1f, 1f);
            closeBtnRect.pivot            = new Vector2(1f, 1f);
            closeBtnRect.anchoredPosition = new Vector2(-10 * _scale, -10 * _scale);
            closeBtnRect.sizeDelta        = new Vector2(32 * _scale, 32 * _scale);
            closeBtn.AddComponent<Image>().color = new Color(0.25f, 0.25f, 0.25f, 1f);
            closeBtn.AddComponent<Button>().onClick.AddListener(() => Destroy(overlay));

            var closeTxt = CreateUIObject("XLabel", closeBtn.transform).AddComponent<TextMeshProUGUI>();
            closeTxt.text      = "X";
            closeTxt.font      = Font;
            closeTxt.fontSize  = 14 * _scale;
            closeTxt.color     = Color.white;
            closeTxt.alignment = TextAlignmentOptions.Center;
            closeTxt.rectTransform.anchorMin = Vector2.zero;
            closeTxt.rectTransform.anchorMax = Vector2.one;
            closeTxt.rectTransform.offsetMin = Vector2.zero;
            closeTxt.rectTransform.offsetMax = Vector2.zero;

            // --- Title ---
            var titleObj = CreateUIObject("Title", panel.transform);
            titleObj.AddComponent<LayoutElement>().preferredHeight = 36 * _scale;
            var titleText = titleObj.AddComponent<TextMeshProUGUI>();
            titleText.text      = "Open Checkout";
            titleText.font      = Font;
            titleText.fontSize  = 20 * _scale;
            titleText.fontStyle = FontStyles.Bold;
            titleText.color     = Color.white;
            titleText.alignment = TextAlignmentOptions.Center;

            // --- Body text ---
            var bodyObj = CreateUIObject("Body", panel.transform);
            bodyObj.AddComponent<LayoutElement>().preferredHeight = 50 * _scale;
            var bodyText = bodyObj.AddComponent<TextMeshProUGUI>();
            bodyText.text            = "Scan the QR code or click to open checkout in your browser.";
            bodyText.font            = Font;
            bodyText.fontSize        = 14 * _scale;
            bodyText.color           = new Color(0.75f, 0.75f, 0.75f, 1f);
            bodyText.alignment       = TextAlignmentOptions.Center;
            bodyText.textWrappingMode = TextWrappingModes.Normal;

            // --- QR code ---
            // Container height reserves the space; AspectRatioFitter keeps the image square.
            if (!string.IsNullOrEmpty(ActiveBasket.links.checkout))
            {
                var qrContainer = CreateUIObject("QRContainer", panel.transform);
                qrContainer.AddComponent<LayoutElement>().preferredHeight = qrSize;

                var qrImg = CreateUIObject("QRImage", qrContainer.transform).AddComponent<RawImage>();
                qrImg.texture = Utilities.QrToTexture(QrCode.Encode(ActiveBasket.links.checkout));
                var qrRect    = qrImg.rectTransform;
                qrRect.anchorMin = Vector2.zero;
                qrRect.anchorMax = Vector2.one;
                qrRect.offsetMin = Vector2.zero;
                qrRect.offsetMax = Vector2.zero;
                var arf = qrImg.gameObject.AddComponent<AspectRatioFitter>();
                arf.aspectMode  = AspectRatioFitter.AspectMode.FitInParent;
                arf.aspectRatio = 1f; // QR codes are always square
            }

            // --- Open in Browser button ---
            var browserBtn = CreateUIObject("BrowserBtn", panel.transform);
            browserBtn.AddComponent<Image>().color = Color.orange;
            browserBtn.AddComponent<LayoutElement>().preferredHeight = 44 * _scale;
            browserBtn.AddComponent<Button>().onClick.AddListener(() => Application.OpenURL(ActiveBasket.links.checkout));

            var browserTxt = CreateUIObject("Text", browserBtn.transform).AddComponent<TextMeshProUGUI>();
            browserTxt.text             = "Open Checkout in Browser";
            browserTxt.font             = Font;
            browserTxt.fontSize         = 14 * _scale;
            browserTxt.color            = Color.black;
            browserTxt.alignment        = TextAlignmentOptions.Center;
            browserTxt.verticalAlignment = VerticalAlignmentOptions.Middle;
            var browserTxtRect  = browserTxt.rectTransform;
            browserTxtRect.anchorMin = Vector2.zero;
            browserTxtRect.anchorMax = Vector2.one;
            browserTxtRect.offsetMin = Vector2.zero;
            browserTxtRect.offsetMax = Vector2.zero;
        }

        private void CreateInputField(Transform parent, string placeholder, Action<string> onSubmit)
        {
            var obj   = CreateUIObject(placeholder, parent);
            var input = obj.AddComponent<TMP_InputField>();
            var text  = CreateUIObject("Text", obj.transform).AddComponent<TextMeshProUGUI>();
            input.textComponent = text;
            input.onEndEdit.AddListener(val => onSubmit(val));
            obj.AddComponent<LayoutElement>().preferredHeight = 40 * _scale;
        }

        private GameObject CreateUIObject(string name, Transform parent)
        {
            var obj = new GameObject(name);
            obj.transform.SetParent(parent, false);
            obj.AddComponent<RectTransform>();
            return obj;
        }

        #endregion

        #region Animation

        public void Open()
        {
            if (cartRect == null) return;

            if (animRoutine != null) StopCoroutine(animRoutine);
            animRoutine = StartCoroutine(Slide(new Vector2(0, 0)));  // fully on-screen

            isOpen = true;
        }

        public void Close()
        {
            if (cartRect == null) return;

            if (animRoutine != null) StopCoroutine(animRoutine);
            animRoutine = StartCoroutine(Slide(new Vector2(_cartWidth, 0))); // off-screen to the right

            isOpen = false;
        }

        private IEnumerator Slide(Vector2 target)
        {
            float t = 0;
            Vector2 start = cartRect.anchoredPosition;

            while (t < 0.25f)
            {
                t += Time.deltaTime;
                cartRect.anchoredPosition = Vector2.Lerp(start, target, t / 0.25f);
                yield return null;
            }

            cartRect.anchoredPosition = target;
        }

        #endregion
    }
}
