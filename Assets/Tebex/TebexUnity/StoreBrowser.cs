using System;
using System.Globalization;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using JetBrains.Annotations;
using Tebex.Headless;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Tebex.TebexUnity
{
    public class StoreBrowser : MonoBehaviour
    {
        [Header("Config")]
        [SerializeField] public string storePublicKey;
        [SerializeField] public Canvas canvas;
        [SerializeField] public bool useInGameCart = true;
        [SerializeField] private InGameCart cart;

        [Header("Style")]
        [SerializeField] public Texture2D storeIcon;
        [SerializeField] public Texture2D storeBackground;
        [SerializeField] public Color32 storeTextColor = Color.white;
        [SerializeField] public TMP_FontAsset storeFont;
        [SerializeField] public TMP_FontAsset productHeaderFont;
        [SerializeField] public TMP_FontAsset productFont;
        [SerializeField] public int headerTextSize = 24;
        [SerializeField] public int storeTextSize = 16;
        [SerializeField] public int storePackagesPerRow = 3;
        [SerializeField] public int storeRefreshIntervalMinutes = 30;

        [CanBeNull] private Webstore _webstore;

        private DateTime _lastRefreshed;
        private Category _activeCategory;
        private bool _needsRedraw;

        // Computed each DrawUI call from the canvas's actual coordinate space
        private float _canvasWidth;
        private float _canvasHeight;
        private float _headerHeight;
        private float _sidebarWidth;
        private float _categoryBarHeight;

        private void Start()
        {
            HeadlessApi.GetInstance(new UnityHeadlessAdapter(), storePublicKey);
            Open();
        }

        private void Open()
        {
            if (DateTime.Now - _lastRefreshed > TimeSpan.FromMinutes(storeRefreshIntervalMinutes))
            {
                Debug.Log("Refreshing store data");
                FetchStoreData();
            }

            if (useInGameCart)
            {
                cart.NewBasketWithEmail("john.doe@overwolf.com");
            }

            if (Tebex.Categories.Count > 0)
                _activeCategory = Tebex.Categories[0];
        }

        private void Update()
        {
            if (_needsRedraw)
            {
                _needsRedraw = false;
                DrawUI();
            }
        }

        public void DrawUI()
        {
            if (canvas == null) return;

            // Read the canvas's coordinate space from its RectTransform.
            // With CanvasScaler "Scale With Screen Size" the rect is already in scaled canvas units,
            // so all layout values below are resolution-independent.
            var canvasRt = canvas.GetComponent<RectTransform>();
            _canvasWidth       = canvasRt.rect.width  > 0 ? canvasRt.rect.width  : 1920f;
            _canvasHeight      = canvasRt.rect.height > 0 ? canvasRt.rect.height : 1080f;
            _headerHeight      = _canvasHeight * 0.10f; // top 10% of the canvas
            _sidebarWidth      = 0f;                    // no sidebar — categories are horizontal
            _categoryBarHeight = _canvasHeight * 0.09f; // horizontal category bar below header

            // CLEANUP
            foreach (Transform child in canvas.transform)
                Destroy(child.gameObject);

            // Full-canvas background image
            if (storeBackground != null)
            {
                var storeBg = new GameObject("StoreBg");
                storeBg.transform.SetParent(canvas.transform, false);
                var sb = storeBg.AddComponent<RectTransform>();
                sb.anchorMin = Vector2.zero;
                sb.anchorMax = Vector2.one;
                sb.offsetMin = Vector2.zero;
                sb.offsetMax = Vector2.zero;
                var sbImage = storeBg.AddComponent<RawImage>();
                sbImage.texture = storeBackground;
                // Decorative background — must not absorb scroll/click events or the
                // ScrollRect below stops receiving wheel input whenever the cursor
                // drifts over a gap between cards.
                sbImage.raycastTarget = false;
            }

            DrawCategories();
            DrawPackages();
            DrawHeader();
        }

        private void DrawHeader()
        {
            // Horizontal bar anchored to the full width of the canvas top
            var header = new GameObject("HeaderBg");
            header.transform.SetParent(canvas.transform, false);

            var hr = header.AddComponent<RectTransform>();
            hr.anchorMin = new Vector2(0, 1);
            hr.anchorMax = new Vector2(1, 1);
            hr.pivot = new Vector2(0.5f, 1f);
            hr.offsetMin = new Vector2(0, -_headerHeight);
            hr.offsetMax = new Vector2(0, 0);

            header.AddComponent<Image>().color = new Color(0, 0, 0, 0.5f);

            var hLayout = header.AddComponent<HorizontalLayoutGroup>();
            hLayout.padding = new RectOffset(10, 10, 5, 5);
            hLayout.spacing = 10;
            hLayout.childAlignment = TextAnchor.MiddleLeft;
            hLayout.childForceExpandWidth = false;
            hLayout.childForceExpandHeight = true;
            hLayout.childControlWidth = true;
            hLayout.childControlHeight = true;

            // Store icon — only added when the texture is assigned in the Inspector
            if (this.storeIcon != null)
            {
                var iconObj = new GameObject("StoreIcon");
                iconObj.transform.SetParent(header.transform, false);
                iconObj.AddComponent<RawImage>().texture = this.storeIcon;
                var iconElem = iconObj.AddComponent<LayoutElement>();
                iconElem.preferredWidth  = _headerHeight - 10f;
                iconElem.preferredHeight = _headerHeight - 10f;
                iconElem.flexibleWidth   = 0;
                iconElem.flexibleHeight  = 0;
            }
            else
            {
                Debug.LogWarning("[StoreBrowser] storeIcon is not assigned in the Inspector — no logo will appear.");
            }

            // Breadcrumb fills remaining space, pushing account/basket to the right
            var breadcrumb = new GameObject("Breadcrumb");
            breadcrumb.transform.SetParent(header.transform, false);
            var bcText = breadcrumb.AddComponent<TextMeshProUGUI>();
            bcText.text = "Menu • Store • " + (_activeCategory != null ? _activeCategory.name : "All Items");
            bcText.color = storeTextColor;
            bcText.fontSize = storeTextSize * 0.70f;
            bcText.font = storeFont;
            bcText.alignment = TextAlignmentOptions.Left;
            breadcrumb.AddComponent<LayoutElement>().flexibleWidth = 1;

            // Account name
            var accountName = new GameObject("Account");
            accountName.transform.SetParent(header.transform, false);
            var anText = accountName.AddComponent<TextMeshProUGUI>();
            anText.text = "john.doe@overwolf.com";
            anText.color = storeTextColor;
            anText.fontSize = 22;
            anText.font = productFont;
            anText.alignment = TextAlignmentOptions.Right;
            var anElem = accountName.AddComponent<LayoutElement>();
            anElem.preferredWidth = _canvasWidth * 0.18f;
            anElem.flexibleWidth = 0;

            // Basket / Webstore button
            // The wrapper takes the full-height layout slot; the inner button is
            // sized to ~60 % of header height and centred vertically so it looks
            // like a real button rather than a bar spanning the whole navbar.
            var basketWrapper = new GameObject("BasketWrapper");
            basketWrapper.transform.SetParent(header.transform, false);
            basketWrapper.AddComponent<RectTransform>();
            var wrapperElem = basketWrapper.AddComponent<LayoutElement>();
            wrapperElem.preferredWidth = _canvasWidth * 0.10f;
            wrapperElem.flexibleWidth  = 0;

            var basketBtn = new GameObject("BasketButton");
            basketBtn.transform.SetParent(basketWrapper.transform, false);
            float btnHeight = _headerHeight * 0.60f;
            var btnRect = basketBtn.AddComponent<RectTransform>();
            btnRect.anchorMin = new Vector2(0.05f, 0.5f);
            btnRect.anchorMax = new Vector2(0.95f, 0.5f);
            btnRect.pivot     = new Vector2(0.5f,  0.5f);
            btnRect.sizeDelta = new Vector2(0f, btnHeight);

            var image = basketBtn.AddComponent<Image>();
            image.color = Color.orange;

            var btn = basketBtn.AddComponent<Button>();
            btn.targetGraphic = image;
            btn.onClick.AddListener(() =>
            {
                if (useInGameCart)
                {
                    if (cart != null)
                    {
                        cart.DrawUI();
                        cart.Open();
                    }
                }
                else
                {
                    Application.OpenURL("https://tebex.io/");
                }
            });

            var textGo = new GameObject("Text");
            textGo.transform.SetParent(basketBtn.transform, false);
            var textRect = textGo.AddComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = Vector2.zero;
            textRect.offsetMax = Vector2.zero;
            var text = textGo.AddComponent<TextMeshProUGUI>();
            text.text = useInGameCart ? "Basket" : "Webstore";
            text.font = productHeaderFont;
            text.fontSize = storeTextSize;
            text.color = Color.black;
            text.fontWeight = FontWeight.Bold;
            text.alignment = TextAlignmentOptions.Center;
        }

        private void DrawCategories()
        {
            // Full-width horizontal bar anchored below the header
            var container = new GameObject("CategoryContainer");
            container.transform.SetParent(canvas.transform, false);

            var containerRect = container.AddComponent<RectTransform>();
            containerRect.anchorMin = new Vector2(0, 1);
            containerRect.anchorMax = new Vector2(1, 1);
            containerRect.pivot     = new Vector2(0.5f, 1f);
            containerRect.offsetMin = new Vector2(0, -(_headerHeight + _categoryBarHeight));
            containerRect.offsetMax = new Vector2(0, -_headerHeight);

            // ScrollRect enables horizontal scrolling when there are many categories
            var scrollRect = container.AddComponent<ScrollRect>();
            scrollRect.horizontal        = true;
            scrollRect.vertical          = false;
            scrollRect.scrollSensitivity = 4f;
            scrollRect.movementType      = ScrollRect.MovementType.Clamped;

            // Viewport clips the scrolling content
            var viewport = new GameObject("Viewport");
            viewport.transform.SetParent(container.transform, false);
            var vpRect = viewport.AddComponent<RectTransform>();
            vpRect.anchorMin = Vector2.zero;
            vpRect.anchorMax = Vector2.one;
            vpRect.offsetMin = Vector2.zero;
            vpRect.offsetMax = Vector2.zero;
            viewport.AddComponent<Image>().color = Color.clear;
            

            // Content grows horizontally; centered anchor keeps it centred when it fits
            var content = new GameObject("Content");
            content.transform.SetParent(viewport.transform, false);
            var contentRect = content.AddComponent<RectTransform>();
            contentRect.anchorMin = new Vector2(0.5f, 0f);
            contentRect.anchorMax = new Vector2(0.5f, 1f);
            contentRect.pivot     = new Vector2(0.5f, 0.5f);
            contentRect.sizeDelta = Vector2.zero;

            var hLayout = content.AddComponent<HorizontalLayoutGroup>();
            hLayout.padding              = new RectOffset(20, 20, 8, 8);
            hLayout.spacing              = 15f;
            hLayout.childAlignment       = TextAnchor.MiddleCenter;
            hLayout.childForceExpandWidth  = false;
            hLayout.childForceExpandHeight = true;
            hLayout.childControlWidth    = false; // buttons size themselves via ContentSizeFitter
            hLayout.childControlHeight   = true;

            content.AddComponent<ContentSizeFitter>().horizontalFit = ContentSizeFitter.FitMode.PreferredSize;

            scrollRect.viewport = vpRect;
            scrollRect.content  = contentRect;

            float btnHeight = _categoryBarHeight * 0.72f;

            // "All Items" button
            AddCategoryButton(content.transform, "All Items", _activeCategory == null, btnHeight, () =>
            {
                _activeCategory = null;
                DrawUI();
            });

            foreach (var category in Tebex.Categories)
            {
                var cat = category; // closure capture
                AddCategoryButton(content.transform, category.name, category == _activeCategory, btnHeight, () =>
                {
                    _activeCategory = cat;
                    DrawUI();
                });
            }
        }

        private void AddCategoryButton(Transform parent, string label, bool isActive, float height,
            UnityEngine.Events.UnityAction onClick)
        {
            var btnObj = new GameObject("Cat_" + label);
            btnObj.transform.SetParent(parent, false);

            // Height only — width is driven by text content via ContentSizeFitter
            var elem = btnObj.AddComponent<LayoutElement>();
            elem.preferredHeight = height;
            elem.flexibleHeight  = 0;

            var btn = btnObj.AddComponent<Button>();
            btn.onClick.AddListener(onClick);

            // HLayout provides horizontal padding; ContentSizeFitter sizes button to text + padding
            var btnLayout = btnObj.AddComponent<HorizontalLayoutGroup>();
            btnLayout.padding              = new RectOffset(18, 18, 0, 0);
            btnLayout.childAlignment       = TextAnchor.MiddleCenter;
            btnLayout.childControlWidth    = true;
            btnLayout.childControlHeight   = true;
            btnLayout.childForceExpandWidth  = false;
            btnLayout.childForceExpandHeight = false;

            btnObj.AddComponent<ContentSizeFitter>().horizontalFit = ContentSizeFitter.FitMode.PreferredSize;

            var textGo = new GameObject("Text");
            textGo.transform.SetParent(btnObj.transform, false);

            var tmp = textGo.AddComponent<TextMeshProUGUI>();
            tmp.text              = "<uppercase>" + label + "</uppercase>";
            tmp.color             = storeTextColor;
            tmp.font              = storeFont;
            tmp.fontSize          = headerTextSize - 2;
            tmp.alignment         = TextAlignmentOptions.Center;
            tmp.textWrappingMode  = TextWrappingModes.NoWrap;
            tmp.overflowMode      = TextOverflowModes.Overflow;
            
            if (isActive)
            {
                tmp.color = Color.cadetBlue;
            }
        }

        private void DrawPackages()
        {
            // Mirror InGameCart's narrow-canvas detection: a portrait-orientation canvas
            // is treated as a phone, and we force a 2-column grid so cards stay tappable.
            bool isNarrow = _canvasWidth < _canvasHeight;
            int perRow = isNarrow ? 2 : storePackagesPerRow;

            // Cell width fills the full canvas width divided by column count
            float contentWidth = _canvasWidth;
            float cellWidth  = (contentWidth / perRow) * 0.93f;
            float cellHeight = cellWidth * 1.15f;

            // Scroll view anchored below the category bar, full canvas width
            var scrollView = new GameObject("PackagesScrollView");
            scrollView.transform.SetParent(canvas.transform, false);

            var scrollViewRect = scrollView.AddComponent<RectTransform>();
            scrollViewRect.anchorMin = new Vector2(0, 0);
            scrollViewRect.anchorMax = new Vector2(1, 1);
            scrollViewRect.pivot = new Vector2(0, 1);
            scrollViewRect.offsetMin = new Vector2(0, 0);
            scrollViewRect.offsetMax = new Vector2(0, -(_headerHeight + _categoryBarHeight));

            var scroll = scrollView.AddComponent<ScrollRect>();
            scroll.vertical   = true;
            scroll.horizontal = false;
            scroll.scrollSensitivity = 10.0f;
            scroll.elasticity = 0.0f;
            scroll.movementType = ScrollRect.MovementType.Clamped;
            
            // Viewport (clips content)
            var viewport = new GameObject("Viewport");
            viewport.transform.SetParent(scrollView.transform, false);
            var vpRect = viewport.AddComponent<RectTransform>();
            vpRect.anchorMin = Vector2.zero;
            vpRect.anchorMax = Vector2.one;
            vpRect.offsetMin = Vector2.zero;
            vpRect.offsetMax = Vector2.zero;
            // Transparent raycast target so scroll-wheel events over gaps between cards
            // still bubble up to the ScrollRect instead of falling through to the canvas.
            var vpImage = viewport.AddComponent<Image>();
            vpImage.color = new Color(0f, 0f, 0f, 0f);
            viewport.AddComponent<RectMask2D>();

            // Content (the actual grid that grows as packages are added)
            var gridContainer = new GameObject("PackagesGrid");
            gridContainer.transform.SetParent(viewport.transform, false);

            var gridRect = gridContainer.AddComponent<RectTransform>();
            gridRect.anchorMin = new Vector2(0, 1);
            gridRect.anchorMax = new Vector2(1, 1);
            gridRect.pivot     = new Vector2(0.5f, 1f);
            gridRect.sizeDelta = Vector2.zero;

            var gridLayout = gridContainer.AddComponent<GridLayoutGroup>();
            gridLayout.cellSize = new Vector2(cellWidth, cellHeight);
            gridLayout.spacing = new Vector2(20, 20);
            gridLayout.startCorner = GridLayoutGroup.Corner.UpperLeft;
            gridLayout.startAxis = GridLayoutGroup.Axis.Horizontal;
            gridLayout.childAlignment = TextAnchor.UpperCenter;
            gridLayout.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            gridLayout.constraintCount = perRow;
            // Compute symmetric side padding so the grid is centred within the full canvas width
            float totalRowWidth = perRow * cellWidth + (perRow - 1) * 20f;
            int sidePad = Mathf.RoundToInt((contentWidth - totalRowWidth) / 2f);
            gridLayout.padding = new RectOffset(sidePad, sidePad, 20, 20);
            gridContainer.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            scroll.viewport = vpRect;
            scroll.content  = gridRect;

            float imgHeight  = cellHeight * 0.38f;
            float btnHeight  = cellHeight * 0.13f;
            float textHeight = cellHeight * 0.12f;
            int   cardPad    = Mathf.RoundToInt(cellWidth * 0.04f);

            foreach (var package in Tebex.Packages)
            {
                if (_activeCategory != null && package.category.id != _activeCategory.id)
                {
                    continue;
                }

                var packObj = new GameObject("Package_" + package.name);
                packObj.transform.SetParent(gridContainer.transform, false);
                packObj.AddComponent<Image>().color = new Color(0, 0, 0, 0.5f);

                var packLayout = packObj.AddComponent<VerticalLayoutGroup>();
                packLayout.padding = new RectOffset(cardPad, cardPad, cardPad, cardPad);
                packLayout.spacing = cellHeight * 0.02f;
                packLayout.childAlignment = TextAnchor.UpperCenter;
                packLayout.childForceExpandWidth = true;
                packLayout.childForceExpandHeight = false;
                packLayout.childControlWidth = true;
                packLayout.childControlHeight = true;

                // Package image — AspectRatioFitter cannot be a direct child of a LayoutGroup,
                // so we wrap it in a plain container that the VLG manages for height.
                var imgContainer = new GameObject("ImageContainer");
                imgContainer.transform.SetParent(packObj.transform, false);
                imgContainer.AddComponent<RectTransform>();
                var imgContElem = imgContainer.AddComponent<LayoutElement>();
                imgContElem.preferredHeight = imgHeight;
                imgContElem.flexibleHeight  = 0;

                var imgObj = new GameObject("Image");
                imgObj.transform.SetParent(imgContainer.transform, false);
                var imgObjRect = imgObj.AddComponent<RectTransform>();
                imgObjRect.anchorMin = Vector2.zero;
                imgObjRect.anchorMax = Vector2.one;
                imgObjRect.offsetMin = Vector2.zero;
                imgObjRect.offsetMax = Vector2.zero;
                var img = imgObj.AddComponent<RawImage>();
                var tex = Tebex.GetTexture(package);
                img.texture = tex;
                if (tex != null && tex.width > 0 && tex.height > 0)
                {
                    var arf = imgObj.AddComponent<AspectRatioFitter>();
                    arf.aspectMode = AspectRatioFitter.AspectMode.FitInParent;
                    arf.aspectRatio = (float)tex.width / tex.height;
                }

                // Title
                CreateText(packObj.transform, package.name, productHeaderFont, storeTextSize, textHeight);

                // Description
                CreateText(packObj.transform, _stripHtml(package.description), productFont, storeTextSize - 8, textHeight);

                // Button row: price button (fills width) + optional "+" add-to-cart button
                var btnRow = new GameObject("ButtonRow");
                btnRow.transform.SetParent(packObj.transform, false);
                var btnRowElem = btnRow.AddComponent<LayoutElement>();
                btnRowElem.preferredHeight = btnHeight;
                btnRowElem.flexibleHeight  = 0;
                var btnRowLayout = btnRow.AddComponent<HorizontalLayoutGroup>();
                btnRowLayout.spacing              = cellWidth * 0.02f;
                btnRowLayout.childForceExpandWidth  = false;
                btnRowLayout.childForceExpandHeight = true;
                btnRowLayout.childControlWidth    = true;
                btnRowLayout.childControlHeight   = true;

                // Price / buy button — fills the remaining width
                var btnObj = new GameObject("BuyButton");
                btnObj.transform.SetParent(btnRow.transform, false);
                btnObj.AddComponent<Image>().color = Color.softGreen;
                btnObj.AddComponent<LayoutElement>().flexibleWidth = 1;

                var btn = btnObj.AddComponent<Button>();
                btn.onClick.AddListener(() =>
                {
                    if (useInGameCart && cart != null)
                    {
                        cart.AddPackage(package);
                        cart.DrawUI();
                        cart.Open();
                    }
                });

                var btnTextGo = new GameObject("Text").AddComponent<TextMeshProUGUI>();
                btnTextGo.text      = $"{package.total_price.ToString("F2", CultureInfo.InvariantCulture)} {package.currency}";
                btnTextGo.alignment = TextAlignmentOptions.Center;
                btnTextGo.color     = Color.black;
                btnTextGo.transform.SetParent(btnObj.transform, false);
                btnTextGo.fontSize  = storeTextSize + 4;   // slightly larger than store text
                btnTextGo.fontStyle = FontStyles.Bold;
                btnTextGo.font      = productHeaderFont;
                var btnTxtRect = btnTextGo.GetComponent<RectTransform>();
                btnTxtRect.anchorMin = Vector2.zero;
                btnTxtRect.anchorMax = Vector2.one;
                btnTxtRect.offsetMin = Vector2.zero;
                btnTxtRect.offsetMax = Vector2.zero;

                // "+" add-to-cart button — square, only when the in-game cart is enabled
                if (useInGameCart && cart != null)
                {
                    var addBtn = new GameObject("AddButton");
                    addBtn.transform.SetParent(btnRow.transform, false);
                    addBtn.AddComponent<Image>().color = Color.gray4;
                    var addElem = addBtn.AddComponent<LayoutElement>();
                    addElem.preferredWidth = btnHeight;
                    addElem.flexibleWidth  = 0;

                    var addBtnComp = addBtn.AddComponent<Button>();
                    addBtnComp.onClick.AddListener(() =>
                    {
                        cart.AddPackage(package);
                        cart.DrawUI();
                        cart.Open();
                    });

                    var addTxt = new GameObject("Text").AddComponent<TextMeshProUGUI>();
                    addTxt.text      = "+";
                    addTxt.alignment = TextAlignmentOptions.Center;
                    addTxt.color     = Color.white;
                    addTxt.transform.SetParent(addBtn.transform, false);
                    addTxt.fontSize  = storeTextSize + 6;
                    addTxt.fontStyle = FontStyles.Bold;
                    addTxt.font      = productHeaderFont;
                    var addTxtRect = addTxt.GetComponent<RectTransform>();
                    addTxtRect.anchorMin = Vector2.zero;
                    addTxtRect.anchorMax = Vector2.one;
                    addTxtRect.offsetMin = Vector2.zero;
                    addTxtRect.offsetMax = Vector2.zero;
                }
            }
        }

        private void CreateText(Transform parent, string value, TMP_FontAsset font, int size, float height)
        {
            var obj = new GameObject("Text_" + value);
            obj.transform.SetParent(parent, false);

            var tmp = obj.AddComponent<TextMeshProUGUI>();
            tmp.text = value;
            tmp.font = font;
            tmp.fontSize = size;
            tmp.color = storeTextColor;
            tmp.alignment = TextAlignmentOptions.Left;
            tmp.overflowMode = TextOverflowModes.Ellipsis;

            var layout = obj.AddComponent<LayoutElement>();
            layout.preferredHeight = height;
            layout.flexibleHeight = 0;
        }

        private async Task FetchStoreData()
        {
            if (string.IsNullOrEmpty(storePublicKey))
                throw new Exception("Store public key not set");

            await HeadlessApi.GetInstance().GetAllCategoriesAsync(categories =>
            {
                Tebex.Categories = categories.data;
                _needsRedraw = true;
            }, apiError =>
            {
                Debug.LogError(apiError.title);
            }, serverError =>
            {
                Debug.LogError(serverError.Body);
            });

            await HeadlessApi.GetInstance().GetAllPackagesAsync(packages =>
            {
                Tebex.Packages = packages.data;
                foreach (var package in Tebex.Packages)
                {
                    Tebex.PackageLookup[package.id] = package;
                }
            }, apiError =>
            {
                Debug.LogError(apiError.title);
            }, serverError =>
            {
                Debug.LogError(serverError.Body);
            });

            await Tebex.PreloadTexturesAsync();
            _needsRedraw = true;

            await HeadlessApi.GetInstance().GetWebstoreAsync(webstore =>
            {
                Tebex.Webstore = webstore;
            }, apiError =>
            {
                Debug.LogError(apiError.title);
            }, serverError =>
            {
                Debug.LogError(serverError.Body);
            });
        }

        private static string _stripHtml(string html)
        {
            return Regex.Replace(html, "<.*?>", string.Empty);
        }
    }
}
