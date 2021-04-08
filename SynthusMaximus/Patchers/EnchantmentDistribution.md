### Overview of the Enchantment distribution patcher

#### Old code - List Bindings
* Get list binding from storage
* For each list
  * if FillUpListOnListEnchantmentBinding
    * for each entry
      * if entry is not armor, skip
      * if entry does not have enchantment, skip
      * if armor is excluded from enchantment, skip
      * get armor template
      * if template is enchanted, skip
      * get similar armor
      * for each similar
        * if similar has same enchantment as base, skip
        * create similar armor with same enchantment
        * add to leveled list
  * for each base enchantment
    * resolve base enchantment
    * for each entry
      * if entry is not armor, skip
      * if entry is not enchantment, skip
      * get template
      * find matching mapping of old->new
      * if no match is found, skip
      * create new armor from the same template, new enchantment
      * for each entry in leveled list
       * add entry for new enchanted item, same count and level


#### Old code - Direct match
* For all Armor A
  * If no material or type, skip
  * If no enchantment, skip
  * If enchanted armor excluded (from enchantment list)
  * Get Template
  * If template armor excluded (from enthantment list)
  * for each new enchantment with the same base as A
    * generate a new armor with the same template but with the new enchantment
    * for each leveled list
      * if list is excluded, skip
      * if list has flag "UseAll", skip
      * for each entry
        * if entry is not armor, skip
        * if we've haven't generated armor for this armor, skip
        * insert generated armor 
        
    