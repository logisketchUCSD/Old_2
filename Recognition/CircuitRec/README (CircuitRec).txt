CircuitRec:
-----------
This is a class library, so it needs to be called from another program.  To use this, create a new CircuitRec object.  The constructor needs a domain and a WordList.  The domain needs to be loaded from a text file.  An example is shown below:

NOT 1 1 F F
AND 2 1 M F
DECODER 1 2 M M 1 0 M F
FULLADDER 2 1 F F 1 1 F F

For symbols with inputs and outputs only on two sides, the first two entries are the format.  There is: 1) the name, 2) the number of input allowed, 3) the number of outputs allowed, 4) M if the number of inputs is a minimum number, F if the number of inputs is fixed, and 5) M or F for the output.  If the symbol can have inputs and/or outputs on multiple sides, the second two entries are the format: 1) name, 2) number of inputs allowed on the left side if the orientation was left to right, 3) number of outputs allowed on the right side, 4) M or F for left input, 5) M or F for right input, 6) number of connections allowed on top, 7) number of connections allowed on bottom, 8) M or F for top, 9) M or F for bottom.  Also, the new symbol names have to be added to the enum in OtherClasses.cs called SymbolTypes.  Then, it also needs to be added to the class SymbolFactory in OtherClasses.cs in the format:

else if (symboltype == "INDUCTOR")
{
	isSimple = true;
	symbtypeenum = SymbolTypes.INDUCTOR;
}

isSimple is true for symbols that have one input side and one output side, and false otherwise.  symbtypeenum needs to be changed to the SymbolType that was added in the previous step.

The WordList can be the default one from TextRecognition (TextRecognition.TextRecognition.createLabelWordList()) or it can be loaded from a file with loadLabelWordList(loadLabelStringList(filepath)).  A custom WordList can be loaded by creating a different .txt file to load from (follow the example of WordList.txt).

Next, to recognize the circuit, use the run method.  It takes in a labeled sketch.  All of the recognized results are stored in the CircuitRec object.  So, to recognize:

CircuitRec cr = new CircuitRec(domain,wordlist);
cr.run();

Errors that occur during recognition are stored in the objects or in CircuitRec and are all added to CircuitRec at the end of recognition.  They are in the form of an exception called ParseError.  It holds the elements that caused the error and a message describing the error.

Fields available that hold the recognition results:

Substroke2CircuitMap: maps the substroke's GUID to the label, symbol, or mesh that the substroke belongs to.  This is used for mesh highlighting in the GUI.

Meshes: the meshes in the circuit, which is used by VerilogWriter

Symbols: the symbols in the circuit, which is used by VerilogWriter

Wires: the wires in the circuit, which is used by VerilogWriter

Errors: the errors that occurred during recognition, used by the GUI

The meshes, symbols, and wires fields hold connection information used by VerilogWriter.

Files:
------

1) CircuitRec.cs: The main class for recognition.  Parses the sketch, creates objects of the other classes (Wire, SimpleSymbol, etc.), and calls the functions for recognition.  After recognition is complete, the results are stored in the CircuitRec object.  To use this, call the constructor, then call the Run method.  The data structure used was chosen to be convenient for CircuitSimulatorUI.

2) ComplexSymbol.cs: For symbols that have inputs and outputs on more than just two sides (i.e. mux, flip-flop).  Right now it is there just to handle when those symbols are recognized, but the algorithms are in a very early state.

3) EndPoint.cs: Represents the endpoints of substrokes (mainly Wires).  Used to find the slope and type (left, right, top, bottom, topleft, etc.) of the endpoints for use in the endpoint determination algorithm in the Wire class.

4) Label.cs: Represents labels of wires in the circuit.  Recognizes and stores the text that the strokes of the label represent.

5) Mesh.cs: Respresents groups of Wires (Wires that are connected together).

6) OtherClasses.cs: Many of the base classes are in here (BaseSymbol, BoundingBox) as well as enums and a class to handle the domain file.

7) SimpleSymbol.cs: For symbols that have inputs and outputs on only two sides (i.e. AND, OR, Resistor).  Finds the connections to the symbol.

8) Wire.cs: Represents wires in the circuit (wires can have more than two endpoints.  Finds the endpoints, finds connections of wires to wires, and finds features for the neural net.

9) Non-Code: Holds some documentation, results, and training files for the neural net

10) digital_domain.txt: Domain file for the digital domain.

11) fulltrain467_13: Describes the 13 feature neural net used as a default to determine if a Wire is an input, output, or internal wire.