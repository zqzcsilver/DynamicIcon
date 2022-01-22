using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Graphics;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Terraria;
using Terraria.GameContent.UI.Elements;
using Terraria.ModLoader;
using Terraria.UI;
using Terraria.UI.Chat;

namespace DynamicIcon
{
    public class DynamicIcon : Mod
    {
        private int nameFrame = 0, time = 0, iconFrame = 0;
        public DynamicSpriteFont PrehistoricPower;
        private string[] myNewName;
        private Texture2D[] icon = new Texture2D[8];
        public override void Load()
        {
            PrehistoricPower = GetFont("Fonts/PrehistoricPower");

            //要染色的文本
            string[] s = StringToArray(Name + " v" + ((Version != null) ? Version.ToString() : null));
            //将字符串数组染色后储存到myNewName
            myNewName = StringToColorString(
                //这里是要输入的颜色
                new Color[] { new Color(0, 0, 0), new Color(100, 100, 100), new Color(200, 200, 200), new Color(255, 255, 255) }, 
                //这个s是输入的字符串数组
                s);

            for (int i = 0; i < icon.Length; i++)
            {
                icon[i] = GetTexture($"Icons/Icon_{i}");
            }
            On.Terraria.Main.DrawMenu += Main_DrawMenu;
            base.Load();
        }
        private void Main_DrawMenu(On.Terraria.Main.orig_DrawMenu orig, Main self, GameTime gameTime)
        {
            //以下两行为获取Main.MenuUI的UIState集
            FieldInfo uiStateField = Main.MenuUI.GetType().GetField("_history", BindingFlags.NonPublic | BindingFlags.Instance);
            List<UIState> _history = (List<UIState>)uiStateField.GetValue(Main.MenuUI);
            //使用for遍历UIState集，寻找UIMods类的实例
            for (int x = 0; x < _history.Count; x++)
            {
                //检测当前UIState的类名全称是否是ModLoader的UIMods
                if (_history[x].GetType().FullName == "Terraria.ModLoader.UI.UIMods")
                {
                    //以下两行为获取UIMods的UI部件集
                    FieldInfo elementsField = _history[x].GetType().GetField("Elements", BindingFlags.NonPublic | BindingFlags.Instance);
                    List<UIElement> elements = (List<UIElement>)elementsField.GetValue(_history[x]);

                    //由之前 了解模组选择页面的构成 一节可知，包含了 包含UIList部件的UIPanel 的UIElement第一个被UIMods包含，故此UIElement位于UIMods的部件集的0号索引处
                    //以下两行用于获取UIElement的UI部件集
                    FieldInfo uiElementsField = elements[0].GetType().GetField("Elements", BindingFlags.NonPublic | BindingFlags.Instance);
                    List<UIElement> uiElements = (List<UIElement>)uiElementsField.GetValue(elements[0]);

                    //同理，由 了解模组选择页面的构成 一节可知，UIPanel第一个被UIElements包含，故UIPanel位于UIElement的UI部件集的0号索引处
                    //以下两行用于获取UIPanel的UI部件集
                    FieldInfo myModUIPanelField = uiElements[0].GetType().GetField("Elements", BindingFlags.NonPublic | BindingFlags.Instance);
                    List<UIElement> myModUIPanel = myModUIPanelField.GetValue(uiElements[0]) as List<UIElement>;

                    //同理，由 了解模组选择页面的构成 一节可知，UIList第一个被UIPanel包含，故UIList位于UIPanel的UI部件集的0号索引处
                    UIList uiList = (UIList)myModUIPanel[0];
                    //遍历uiList包含的子部件，寻找我们mod的UIModItem部件
                    for (int i = 0; i < uiList._items.Count; i++)
                    {
                        //反射获取mod实例，检测其是否是我们的mod
                        if (uiList._items[i].GetType().GetField("_mod", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(uiList._items[i]).ToString() == Name)
                        {
                            //以下两行为获取我们mod的UIModItem的UI部件集
                            FieldInfo myUIModItemField = uiList._items[i].GetType().GetField("Elements", BindingFlags.NonPublic | BindingFlags.Instance);
                            List<UIElement> myUIModItem = (List<UIElement>)myUIModItemField.GetValue(uiList._items[i]);

                            float _modIconAdjust = (GetTexture("icon") == null ? 0 : 85);
                            UIElement badUnloader = myUIModItem.Find((UIElement e) => e.ToString() == "Terraria.ModLoader.UI.UIHoverImage" && e.Top.Pixels == 3);
                            //遍历UIModItem的UI部件集
                            for (int j = 0; j < myUIModItem.Count; j++)
                            {
                                string name = DisplayName + " v" + ((Version != null) ? Version.ToString() : null),
                                    myName = myNewName[myNewName.Length - 1 - nameFrame];

                                //动态字体
                                if (((myUIModItem[j] is UIText) && (myUIModItem[j] as UIText).Text == name) || (badUnloader != null && myUIModItem[j] is ModNameText))
                                {
                                    myUIModItem.RemoveAt(j);
                                    ModNameText modNameText = new ModNameText(myName, 1f, Color.White, PrehistoricPower);
                                    modNameText.Left = new StyleDimension(_modIconAdjust + (badUnloader == null ? 0f : 35f), 0f);
                                    modNameText.Top.Pixels = 5f;
                                    uiList._items[i].Append(modNameText);
                                }
                                else if (myUIModItem[j] is ModNameText)
                                {
                                    ((ModNameText)myUIModItem[j]).Text = myName;
                                }

                                //如果当前UI部件是UIImage，且其宽高均为80
                                if (myUIModItem[j] is UIImage && myUIModItem[j].Width.Pixels == 80 && myUIModItem[j].Height.Pixels == 80)
                                {
                                    //修改此UI部件的贴图
                                    (myUIModItem[j] as UIImage).SetImage(icon[iconFrame]);
                                }
                            }
                            //最后按逆序逐一SetValue
                            myUIModItemField.SetValue(uiList._items[i], myUIModItem);
                            myModUIPanel[0] = uiList;
                            myModUIPanelField.SetValue(uiElements[0], myModUIPanel);
                            uiElementsField.SetValue(elements[0], uiElements);
                            elementsField.SetValue(_history[x], elements);
                            uiStateField.SetValue(Main.MenuUI, _history);
                            //退出循环
                            break;
                        }
                    }
                    //退出循环
                    break;
                }
            }
            time++;
            if (time % 6 == 0)
            {
                nameFrame++;
                iconFrame++;
                time = 0;
            }
            if (nameFrame >= myNewName.Length)
                nameFrame = 0;
            if (iconFrame >= icon.Length)
                iconFrame = 0;
            orig(self, gameTime);
        }
        public override void Unload()
        {
            On.Terraria.Main.DrawMenu -= Main_DrawMenu;
            base.Unload();
        }
        //这是动态字体的UI部件
        private class ModNameText : Terraria.UI.UIElement
        {
            public string Text;
            public float Size;
            public Color Color;
            public DynamicSpriteFont DynamicSpriteFont;
            public ModNameText(string text, float size, Color color, DynamicSpriteFont dynamicSpriteFont)
            {
                Text = text;
                Size = size;
                Color = color;
                DynamicSpriteFont = dynamicSpriteFont;
            }
            public override void Draw(SpriteBatch spriteBatch)
            {
                CalculatedStyle dimensions = GetDimensions();
                ChatManager.DrawColorCodedStringWithShadow(spriteBatch, DynamicSpriteFont, Text, new Vector2(dimensions.X, dimensions.Y), Color, 0, Vector2.Zero, new Vector2(Size));
                base.Draw(spriteBatch);
            }
        }
        //给字体染色的函数
        public static string[] StringToColorString(Color[] colors, string[] texts)
        {
            List<Color> sortColor = colors.ToList();
            sortColor.Sort((now, next) => (now.R * 0.299f + now.G * 0.587f + now.B * 0.114f).CompareTo(next.R * 0.299f + next.G * 0.587f + next.B * 0.114f));
            Color[] needColor = new Color[sortColor.Count];
            int iAdd = 0;
            int colorCount = needColor.Length / 2;
            for (int i = 0; i <= colorCount; i++)
            {
                if ((colorCount + i) < needColor.Length)
                {
                    needColor[colorCount + i] = sortColor[iAdd];
                }
                if (iAdd < (sortColor.Count - 1))
                    iAdd++;

                needColor[colorCount - i] = sortColor[iAdd];
                if (iAdd < (sortColor.Count - 1))
                    iAdd++;
            }
            List<string> s = new List<string>();
            string add;
            for (int x = 0; x < needColor.Length; x++)
            {
                add = "";
                for (int i = 0; i < texts.Length; i++)
                {
                    add += "[c/" + needColor[(x + i) % needColor.Length].Hex3() + ":" + texts[i] + "]";
                }
                s.Add(add);
            }
            return s.ToArray();
        }
        //把字符串分为一个个字符
        public static string[] StringToArray(string text)
        {
            string[] s = new string[text.Length];
            for (int i = 0; i < s.Length; i++)
                s[i] = text[i].ToString();
            return s;
        }
    }
}