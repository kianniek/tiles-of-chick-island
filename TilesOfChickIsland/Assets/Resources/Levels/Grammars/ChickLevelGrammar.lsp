version: 0.6f
module:
name: "DoDecorations"
alphabet: "TileAlphabet"
position: (11,-53)
type: Recipe
match: None
inputs: "PlaceTerrain"
grammar: true
recipe: true
showMembers: true

module:
name: "PlaceTerrain"
alphabet: "TileAlphabet"
position: (-96,10)
type: Recipe
match: None
recipe: true
showMembers: true

alphabet:
name: "TileAlphabet"
position: (-145,-83)

module:
name: "DoStartEnd"
alphabet: "TileAlphabet"
position: (9,83)
type: Grammar
match: None
inputs: "PlaceTerrain"
maxIterations: 1
grammar: true
showMembers: true

module:
name: "Recombine"
alphabet: "TileAlphabet"
position: (145,14)
type: None
match: StackTiles
inputs: "DoStartEnd" "DoDecorations"
showMembers: true

