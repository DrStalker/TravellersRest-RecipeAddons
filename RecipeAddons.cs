using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using System;
using static UnityEngine.UIElements.UIRAtlasManager;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Data.SqlTypes;
using System.Net.Http.Headers;

namespace RecipeAddons
{
    [BepInPlugin(PluginInfo.PLUGIN_GUID, PluginInfo.PLUGIN_NAME, PluginInfo.PLUGIN_VERSION)]
    public class Plugin : BaseUnityPlugin
    {
        private static Harmony _harmony;
        internal static ManualLogSource Log;

        private static ConfigEntry<bool> _debugLogging;
        private static ConfigEntry<bool> _allJuiceIsJuice;
        private static ConfigEntry<bool> _allMaltIsMalt;
        private static ConfigEntry<bool> _biggerBurger;
        private static ConfigEntry<bool> _FruitAndVegInterchange;

        public static readonly int firstRecipeId = 831821414;  //The hope being to never conclict with another mod!
        private static int numAddedRecipies = 0;


        public static readonly int s_itemIdJuice = 1325;
        public static readonly int s_itemIdBurgerComplete = 320;
        public static readonly int s_itemIdOnionRings = 1327;
        public static readonly int s_itemIdChili = 1366;
        public static readonly int s_itemIdMayo = 1359;
        public static readonly int s_itemIdSauce = 1272;
        public static readonly int s_itemIdCheeseAny = -4;
        public static readonly int s_itemIdMalt = 1544;
        public static readonly int s_itemIdMaltToasted = 1545;  


        public static readonly int s_recipeBurgerComplete = 320;
        public static readonly int s_recipeBurgerCheese = 321;


        public Plugin()
        {
            // bind to config settings
            _debugLogging = Config.Bind("Debug", "Debug Logging", false, "Logs additional information to console");
            _allJuiceIsJuice = Config.Bind("Recipes", "All juice is juice", false, "Recipies using juice can use any type of juice");
            _biggerBurger = Config.Bind("Recipes", "Bigger Burger", false, "Adds sauce to cheesburger (addExtraIngredient)");
            _FruitAndVegInterchange = Config.Bind("Recipes", "Interchangable Fruit and Veg", false, "Both fruit and veg can be used in any recipe that for either (addExtraTypeToGroup)");
            _allMaltIsMalt = Config.Bind("Recipes", "All malt is malt", false, "Use any type of malt for any recipe (toasted/plain still matters)");
        }

        private void Awake()
        {
            // Plugin startup logic
            Log = Logger;
            _harmony = Harmony.CreateAndPatchAll(typeof(Plugin));
            Logger.LogInfo($"Plugin {PluginInfo.PLUGIN_GUID} is loaded!");
        }

        private void OnDestroy()
        {
            _harmony.UnpatchSelf();
        }
        public static void DebugLog(string message)
        {
            // Log a message to console only if debug is enabled in console
            if (_debugLogging.Value)
            {
                Log.LogInfo(string.Format("Debug: {0}", message));
            }
        }
        // Make some local accessors 
        private static RecipeDatabaseAccessor myRecipeDatabaseAccessor;
        public static RecipeDatabaseAccessor recipeDatabaseAccessor
        {
            get
            {
                if (myRecipeDatabaseAccessor == null) myRecipeDatabaseAccessor = RecipeDatabaseAccessor.GetInstance();
                return myRecipeDatabaseAccessor;
            }
        }
        private static RecipeDatabase myRecipeDatabaseSO;
        public static RecipeDatabase recipeDatabaseSO
        {
            get
            {
                if (myRecipeDatabaseSO == null) myRecipeDatabaseSO = Traverse.Create(recipeDatabaseAccessor).Field("recipeDatabaseSO").GetValue<RecipeDatabase>();
                return myRecipeDatabaseSO;
            }
        }

        private static ItemDatabaseAccessor myItemDatabaseAccessor;
        public static ItemDatabaseAccessor itemDatabaseAccessor
        {
            get
            {
                if (myItemDatabaseAccessor == null) myItemDatabaseAccessor = UnityEngine.Object.FindObjectOfType<ItemDatabaseAccessor>();
                return myItemDatabaseAccessor;
            }
        }
        private static ItemDatabase myitemDatabaseSO;
        public static ItemDatabase itemDatabaseSO
        {
            get
            {
                if (myitemDatabaseSO == null) myitemDatabaseSO = Traverse.Create(itemDatabaseAccessor).Field("itemDatabaseSO").GetValue<ItemDatabase>();
                return myitemDatabaseSO;
            }
        }

        private static int getNextRecipeId()
        {
            return firstRecipeId + numAddedRecipies;
        }

        private static int Item2id(Item x)
        {
            int ingrediantId = Traverse.Create(x).Field("id").GetValue<int>();
            return ingrediantId;
        }




        // ///////////////////////////////////////////////
        // Removes any modifier for this item from any recipe that uses it
        // Call using item, itemId, array of item ids.
        public static bool RemoveModifierFromIngrediant(Item x)
        {
            return RemoveModifierFromIngrediant(Item2id(x));
        }
        public static bool RemoveModifierFromIngrediant(int[] x)
        {
            bool retValue = true;
            for (int i=0; i<x.Length;  i++)
            {
                retValue &= RemoveModifierFromIngrediant(x[i]); //return false if any of these fail
            }
            return retValue;
        }
        public static bool RemoveModifierFromIngrediant(int xId)
        {
            int foundCount = 0;
            for (int i = 0; i < recipeDatabaseSO.recipes.Length; i++)
            {
                //Step through each RecipeIngredient in ingredientsNeeded[]
                for (int j = 0; j < recipeDatabaseSO.recipes[i].ingredientsNeeded.Length; j++)
                {
                    //steal the private ids
                    int ingrediantId    = Item2id(recipeDatabaseSO.recipes[i].ingredientsNeeded[j].item);
                    // int ingrediantmodId = Item2id(recipeDatabaseSO.recipes[i].ingredientsNeeded[j].mod);
                    if (ingrediantId == xId)
                    {
                        foundCount++;
                        recipeDatabaseSO.recipes[i].ingredientsNeeded[j].mod = null; // remove the mod from RecipeIngredient struct
                    }
                }
            }
            DebugLog(String.Format("RemoveModifierFromIngrediant: Removed modifer from {0} occurance of item {1}", foundCount, xId));
            return (foundCount > 0);
        }

        // ///////////////////////////////////////////////
        // Adds an extra Type of ingrediant to an existing Item group.  If used on an IngredientGroup that has items but no group, the group takes precense over the item list.

        public static bool addExtraTypeToGroup(int ingredientGroupId, IngredientType newType)
        {
            IngredientGroup groupItem = (IngredientGroup)ItemDatabaseAccessor.GetItem(ingredientGroupId);
            if (groupItem is null)
            {
                DebugLog(String.Format("addExtraTypeToGroup: Failed to find ingredientGroup with id {0}", ingredientGroupId));
                return false;
            }
            DebugLog(String.Format("addExtraTypeToGroup: Pre: ingredientGroup {0}: type count {1}", ingredientGroupId, groupItem.ingredientsTypes.Length));
            List<IngredientType> itList = groupItem.ingredientsTypes.ToList();
            itList.Add(newType);
            groupItem.ingredientsTypes = itList.ToArray();
            DebugLog(String.Format("addExtraTypeToGroup: Pst: ingredientGroup {0}: type count {1}", ingredientGroupId, groupItem.ingredientsTypes.Length));
            return true;

        }

        // ///////////////////////////////////////////////
        // Adds an extra ingrediant to an existing recipe. 
        // NOTE: limit 5 ingredients, limtts 3 ingrediants that require the player to choose an option otherwise the crafting GUI throws erorrs
        public static bool addExtraIngredient(int recipeId, Item item, int amount = 1, Item mod = null)
        {
            Recipe r = RecipeDatabaseAccessor.GetRecipe(recipeId);
            DebugLog(String.Format("addExtraIngredient: Looking for recipe {0} int {1} known recipes", recipeId, recipeDatabaseSO.recipes.Length));
            bool found = false;
            for (int i = 0; i < recipeDatabaseSO.recipes.Length; i++)
            {
                int reflectedId = Traverse.Create(recipeDatabaseSO.recipes[i]).Field("id").GetValue<int>();
                if (reflectedId == recipeId)
                {
                    found = true;
                    DebugLog(String.Format("addExtraIngredient: Pre: Recipe {0}: ingredient count {1}", reflectedId, recipeDatabaseSO.recipes[i].ingredientsNeeded.Length));
                    RecipeIngredient newI;
                    newI.item = item;
                    newI.amount = amount;
                    newI.mod = mod;
                    List<RecipeIngredient> riList = recipeDatabaseSO.recipes[i].ingredientsNeeded.ToList();
                    riList.Add(newI);
                    recipeDatabaseSO.recipes[i].ingredientsNeeded = riList.ToArray();
                    DebugLog(String.Format("addExtraIngredient: Pst: Recipe {0}: ingredient count {1}", reflectedId, recipeDatabaseSO.recipes[i].ingredientsNeeded.Length));
                    break;
                }
            }
            if (!found) DebugLog(String.Format("addExtraIngredient: Failed to find recipe with id {0}", recipeId));
            return (found);
        }

        // ///////////////////////////////////////////////////////////////////////
        // To-Do functions

        public static bool SwapIngredients(Item from, Item to, bool keepModifiers = false)
        {
            return SwapIngredients(Item2id(from), Item2id(to), keepModifiers);
        }
        public static bool SwapIngredients(int from, int to, bool keepModifiers=false)
        {
            return true;
        }



        public static bool changeOutputAmount(int recipeId, int x, bool addative = false)
        {
            return true;
        }


        public static bool addNewRecipe(Item output, int amount, int craftTime, int Fuel, bool noReuse, ItemMod[] ingrediants, Recipe.RecipeGroup recipeGroup)
        {
            int newId = getNextRecipeId();





            numAddedRecipies++;
            return true;
        }


        // ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
        // Recipe Stuff
        // The Recipe database is not populated ully during Plugin.Awake(), so we attach to the RecipeDatabaseAccessor Awake() function

        [HarmonyPatch(typeof(RecipeDatabaseAccessor), "Awake")]
        [HarmonyPostfix]
        private static void RecipeDatabaseAccessorAwakePostFix(RecipeDatabaseAccessor __instance)
        {
            DebugLog("RecipeDatabaseAccessor.Awake.PostFix");


            //Remove modifiers from all recipes that use that item, so any type of the item may be used (e.g.: any juice instead of grape juice for wine)
            if (_allJuiceIsJuice.Value) RemoveModifierFromIngrediant(s_itemIdJuice);
            if (_allMaltIsMalt.Value) RemoveModifierFromIngrediant(s_itemIdMalt);
            if (_allMaltIsMalt.Value) RemoveModifierFromIngrediant(s_itemIdMaltToasted);


            // Add an ingrediant to an existing recipe
            if (_biggerBurger.Value)
            {
                DebugLog("RecipeDatabaseAccessor.Awake.PostFix: Building a better Burger");
                //Limit three "choice" ingrediants, 5 total?
                addExtraIngredient(s_recipeBurgerCheese, ItemDatabaseAccessor.GetItem(s_itemIdSauce), 1, null);
            }


            // Expand the existing IngredientTypes to include more types
            if (_FruitAndVegInterchange.Value)
            {
                addExtraTypeToGroup(-9, IngredientType.Fruit);
                addExtraTypeToGroup(-2, IngredientType.Veg);
            }

        }
    }
}
