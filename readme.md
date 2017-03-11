# FF6MMGEN 1.0


This small utility does one thing: generating the WOB and WOR mini-maps like FF3Edit does. However, FF3EDit does not take into account a color switch for the Sealed Gate that has different colors in order to mask it with the water color after the continent lifted up.

It also does not take into account location masking for the Sealed Gate entrance, resulting in a white dot in the water. I altered slightly the palette (color 9) and the original color assignation and made a reassignation for a rectangle on the map which represent the Sealed Gate continent. This rectangle can be made custom with the utility. Default coordinate are vanilla Sealed Gate continent.

I had to sacrifice a color for the Sealed Gate continent, so 2 color are reassignated to color 8. This does not make a significant difference. I had to do this to have a matching location color I would mask, it was a tough choice between the two but figured on a custom map the choice I've made could have been the obvious one to do.

### App usage

Default usage example: ff6mmgen.exe -r romname.smc

Cusomt usage example:  ff6mmgen.exe -r romname.smc -x1 40 -x2 48 -y1 50 -y2 62

The custom example will mask a rectangle on the WOB mini-map when contientrise up, defined by (x1,y1), (x1,y2), (x2,y1), (x2,y2).

1. x1 and y1 must be between 0 and 63
2. x2 and y2 must be between 1 and 64
3. x1 must be smaller than x2
4. y1 must be smaller than y2

### ROM Area affected

```
$EFE49B-$EFE8B2 	World of Balance Minimap Graphics
$EFE8B3-$EFED25 	World of Ruin Minimap Graphics
$EFED26-$EFFAC7 	Daryl's Airship Graphics (relocated right after WOR mini-map)
$EFFAC8-$EFFBC7 	Palettes for Ending Airship Scene (relocated right after Airship GFX)
$EFFBC8-$EFFEEF 	Unused Space (beginning could be used due to data shifting)
```

### Code changes

```
$EE9B0E:    NOP
$EE9B0F:    JSR $B1F7
$EE9B12:    PLP
$EE9B13:    RTS

$EEB1F7:    STA $7EE1B0
$EEB1FB:    STA $7EE1B2
$EEB1FF:    RTS
```


### Palette colors (RAM)

- M = used on modified mini-maps
- S = used for Sealed Gate contient

```
ID  Offset   Description             Hex value  RGB value
---------------------------------------------------------
1-  $D2EEA2: sea (M)                  $1084     (4,4,4)
2-  $D2EEA4: land, dark (M)           $294A     (10,10,10)
3-  $D2EEA6: land, medium (M)         $35AD     (13,13,13)
4-  $D2EEA8: mountain edge (M)        $4E73     (19,19,19)
5-  $D2EEAA: location (M)             $7FFF     (31,31,31)
6-  $D2EEAC: land, dark (S)           $294A     (10,10,10)
7-  $D2EEAE: land, medium (S)         $35AD     (13,13,13)
8-  $D2EEB0: mountain edge (S)        $4E73     (19,19,19)
9-  $D2EEB0: location (S)             $7FFF     (31,31,31)
10- $D2EEB4: mountain edge, light (M) $5AD6     (22,22,22)
```

### Sealed Gate color switch

```
color 2  -> color 6
color 3  -> color 7
color 4  -> color 8
color 5  -> color 9
color 10 -> color 8
```

### RAM color switch

```
color 1 -> color 6
color 1 -> color 7
color 1 -> color 8
color 1 -> color 9
```

### Credits

- LeetSketcher for Mini-Map palette findings
- Yousei for FF3Edit source code (compression and minimap generation)
- giangurgolo for Zone Doctor source code (Rom and Bits classes)

### Version History

1.1 - 03/10/2017 - Palette tweak, utility exception handling improvements
1.0 - 02/28/2017 - Initial release

### Infos

contact: themadsiur@gmail.com

website: http://madsiur.net

[FF6hacking.com Thread](http://www.ff6hacking.com/forums/showthread.php?tid=3368)






