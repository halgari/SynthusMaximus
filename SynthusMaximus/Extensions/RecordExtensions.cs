using System.IO;
using Mutagen.Bethesda;
using Mutagen.Bethesda.Skyrim;
using System.Linq;
using Noggog;

namespace SynthusMaximus
{
    public static class RecordExtensions
    {
        public static bool HasAnyKeyword(this IKeywordedGetter<IKeywordGetter> a, params IFormLink<IKeywordGetter>[] kws)
        {
            return kws.Any(a.HasKeyword);
        }

        
        /// <summary>
        /// Adds a consumed item to the recipe 
        /// </summary>
        /// <param name="cobj"></param>
        /// <param name="item">Item to consume</param>
        /// <param name="count">Item count</param>
        public static void AddCraftingRequirement(this ConstructibleObject cobj, IFormLink<IItemGetter> item, int count)
        {
            cobj.Items ??= new ExtendedList<ContainerEntry>();
            cobj.Items.Add(new ContainerEntry()
            {
                Item = new ContainerItem()
                {
                    Item = item,
                    Count = count
                }
            });
        }
        
        /// <summary>
        /// Adds a consumed item to the recipe 
        /// </summary>
        /// <param name="cobj"></param>
        /// <param name="item">Item to consume</param>
        /// <param name="count">Item count</param>
        public static void AddCraftingRequirement(this ConstructibleObject cobj, IItemGetter item, int count)
        {
            cobj.AddCraftingRequirement(new FormLink<IItemGetter>(item), count);
        }

        /// <summary>
        /// Adds a condition to the recipe that an item exist in the player's inventory
        /// </summary>
        /// <param name="cobj"></param>
        /// <param name="item"></param>
        /// <param name="count"></param>
        public static void AddCraftingInventoryCondition(this ConstructibleObject cobj, IFormLink<ISkyrimMajorRecordGetter> item, int count = 1)
        {
            cobj.Conditions.Add(new ConditionFloat
            {
                Data = new FunctionConditionData
                {
                    Function = Condition.Function.GetItemCount,
                    ParameterOneRecord = item
                },
                CompareOperator = CompareOperator.GreaterThanOrEqualTo,
                ComparisonValue = count
            });
        }

        /// <summary>
        /// Adds a condition to the recipe that the user have a given perk
        /// </summary>
        /// <param name="cobj"></param>
        /// <param name="perk"></param>
        public static void AddCraftingPerkCondition(this ConstructibleObject cobj, IFormLink<IPerkGetter> perk, bool mustHave = true)
        {
            cobj.Conditions.Add(new ConditionFloat
            {
                Data = new FunctionConditionData
                {
                    Function = Condition.Function.HasPerk,
                    ParameterOneRecord = perk,
                },
                CompareOperator = CompareOperator.EqualTo,
                ComparisonValue = mustHave ? 1 : 0,
            });
        }

        public static string NameOrThrow(this ITranslatedNamedGetter getter)
        {
            if (getter.Name == null || !getter.Name!.TryLookup(Language.English, out var name) || name == null)
                throw new InvalidDataException($"Cannot get English name from {getter}");
            return name!;
        }
        
        public static string NameOrEmpty(this ITranslatedNamedGetter getter)
        {
            if (getter.Name == null || !getter.Name!.TryLookup(Language.English, out var name) || name == null)
                return "";
            return name!;
        }

        
        public static string NameOrThrow(this ITranslatedStringGetter? getter)
        {
            if (getter == null || !getter!.TryLookup(Language.English, out var name) || name == null)
                throw new InvalidDataException($"Cannot get English name from {getter}");
            return name!;
        }
        
        public static string NameOrEmpty(this ITranslatedStringGetter? getter)
        {
            if (getter == null || !getter!.TryLookup(Language.English, out var name) || name == null)
                return "";
            return name!;
        }

        public static void AddCraftingInventoryCondition(this ConstructibleObject cobj, IItemGetter? item, int count = 1)
        {
            cobj.AddCraftingInventoryCondition(new FormLink<ISkyrimMajorRecordGetter>(item.FormKey), count);
        }

        public static ScriptEntry GetOrAddScript(this Weapon vm, string script)
        {
            vm.VirtualMachineAdapter ??= new VirtualMachineAdapter();
            
            var se = vm.VirtualMachineAdapter.Scripts.FirstOrDefault(s => s.Name == script);
            if (se != null)
                return se;
            se = new ScriptEntry {Name = script};
            vm.VirtualMachineAdapter.Scripts.Add(se);
            return se;
        }
    }
}