using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using System;
using static UnityEngine.UIElements.UIRAtlasManager;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace RecipeAddons
{
    [BepInPlugin(PluginInfo.PLUGIN_GUID, PluginInfo.PLUGIN_NAME, PluginInfo.PLUGIN_VERSION)]
    public class Plugin : BaseUnityPlugin
    {
        private static Harmony _harmony;
        internal static ManualLogSource Log;

        private static ConfigEntry<bool> _debugLogging;
        private static ConfigEntry<bool> _allJuiceIsJuice;
        private static ConfigEntry<bool> _fruitVodka;
        private static ConfigEntry<bool> _spicedRum;
        private static ConfigEntry<bool> _biggerBurger;

        public static readonly int firstRecipeId = 831821414;  //The hope being to never conclict with another mod!
        private static int numAddedRecipies = 0;


        public static readonly int itemIdJuice = 1325;
        public static readonly int itemIdBurgerComplete = 320;
        public static readonly int itemIdOnionRings = 1327;
        public static readonly int itemIdChili = 1366;
        public static readonly int itemIdMayo = 1359;
        public static readonly int itemIdSauce = 1272;
        public static readonly int itemIdCheeseAny = -4;


        public Plugin()
        {
            // bind to config settings
            _debugLogging = Config.Bind("Debug", "Debug Logging", false, "Logs additional information to console");
            _allJuiceIsJuice = Config.Bind("Recipes", "All juice is juice", false, "Recipies using juice can use any type of juice");
            _fruitVodka = Config.Bind("Recipes", "Fruit vodka", false, "Add recipe for fruit vodka to cocktail table");
            _spicedRum = Config.Bind("Recipes", "Spiced Rum", false, "Add recipe for spiced rum to cocktail table");
            _biggerBurger = Config.Bind("Recipes", "Bigger Burger", false, "complete burger is more complete");

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



        private static int getNextRecipeId()
        {
            return firstRecipeId + numAddedRecipies;
        }

        public static void addExtraIngredient(int recipeId, Item item, int amount = 1, Item mod = null)
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
        }

        public static void changeOutputAmount(int recipeId, int x, bool addative = false)
        {

        }


        public static void addNewRecipe(Item output, int amount, int craftTime, int Fuel, bool noReuse, ItemMod[] ingrediants, Recipe.RecipeGroup recipeGroup)
        {
            int newId = getNextRecipeId();





            numAddedRecipies++;
        }


        // ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
        // Recipe Stuff
        // The Recipe database is not accessible during Plugin.Awake(), so we attach to the Accessor Awake() function

        [HarmonyPatch(typeof(RecipeDatabaseAccessor), "Awake")]
        [HarmonyPostfix]
        private static void RecipeDatabaseAccessorAwakePostFix(RecipeDatabaseAccessor __instance)
        {
            DebugLog("RecipeDatabaseAccessor.Awake.PostFix");
            Recipe[] allRecipes = RecipeDatabaseAccessor.GetAllRecipes();
            DebugLog(String.Format("Found {0} recipes", allRecipes.Length));

            for (int i = 0; i < allRecipes.Length; i++)
            {
                if (_allJuiceIsJuice.Value)
                {
                    //Step through each RecipeIngredient in ingredientsNeeded[]
                    for (int j = 0; j < allRecipes[i].ingredientsNeeded.Length; j++)
                    {
                        //steal the private ids
                        int ingrediantId = Traverse.Create(allRecipes[i].ingredientsNeeded[j].item).Field("id").GetValue<int>();
                        int ingrediantmodId = Traverse.Create(allRecipes[i].ingredientsNeeded[j].mod).Field("id").GetValue<int>();
                        if (ingrediantId == itemIdJuice)
                        {
                            allRecipes[i].ingredientsNeeded[j].mod = null; //should be an empty item, not null? GetItem(0) gets null though...
                        }

                    }
                }

            }
            if (_biggerBurger.Value)
            {
                DebugLog("RecipeDatabaseAccessor.Awake.PostFix: Building a better Burger");
                Item onionRings = ItemDatabaseAccessor.GetItem(itemIdOnionRings);
                Item mayo = ItemDatabaseAccessor.GetItem(itemIdMayo);
                Item chili = ItemDatabaseAccessor.GetItem(itemIdChili);
                Item sauce = ItemDatabaseAccessor.GetItem(itemIdSauce);
                Item cheese = ItemDatabaseAccessor.GetItem(itemIdCheeseAny);

                //Limit three "choice" ingrediants, 5 total?
                //addExtraIngredient(321, cheese, 1, null);
                //addExtraIngredient(321, onionRings, 1, null);
                //addExtraIngredient(320, chili, 1, null);
                //addExtraIngredient(320, mayo, 1, null);
                addExtraIngredient(321, sauce, 1, null);
                

            }
        }
    }
}
