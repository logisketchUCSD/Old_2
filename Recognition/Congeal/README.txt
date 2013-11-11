# -------------------------------------------------------------------------- #
################################# CONGEALER ##################################
# -------------------------------------------------------------------------- #

This class is an image-based symbol recognizer. It is based of work done by
Eric G. Learned-Miller; specifically, his paper "Data Driven Image Models
through Continuous Join Alignment." However, this class currently does not
implement the statistical kernel used in the Miller paper (which is really
the meat of the paper), and instead uses a combination of distance metrics
(primarily Hausdorff distances) to compare congealed results.

The most important class for the Congealer is the Designation class. Each
Designation object represents one shape class. There are currently two
optimization algorithms supported for training the Designation objects: Hill
Climbing, and Simulated Annealing. To enable Hill Climbing, the first line
of this file should be "#define HILL_CLIMBING", whereas to enable Simulated
Annealing, the first line should be "#define SIMULATED_ANNEALING".

To train a Designation, simply pass it a list of Bitmaps (or of
TrainingDataPreprocessor.GateImage objects, which wrap a Bitmap and Shape
into a single struct). The training process begins by calling an Aligner to
attempt to get a rough rotational alignment for the images (see the Aligner
directory in Recognition), then using Congealing for fine alignment. Once
the object has been trained, additional bitmaps or shapes can be passed in
and they will be classified by the classify() method.

The classify() method can use several different classification metrics.
Firstly, there are three different Hausdorff distance: Hausdorff (a straight
Hausdorff distance, DirectedPartialHausdorff (which is really Partial
Hausdorff, which ignores the top 6% of farthest away points to increase
noise tolerance), and DirectedModifiedHausdorff (which is really a Modified
Hausdorff, which replaces the Max with a Sum for even better noise
tolerance, but greatly increased variance). There are also three similarity
measures: the Tanimoto Coefficient, the Yule Coefficient, and Entropy.
Tanimoto and Yule are very well described in Levent Burak Kara (CMU)'s paper
"An Image-Based Trainable Symbol Recognizer for Sketch-Based Interfaces".

These are wrapped in CongealRecognizer (under the Recognizers/ directory in
Recognition) into a composite score. If you're going to use Congealing, you
probably want to take a good look at CongealRecognizer.


# ---------------------------- Training ----------------------------------- #

The congealer includes a sample trainer. If you change the project settings
in Visual Studio to build as an EXE instead of a Library, you will get a
program (Congeal.exe) which is built from the Trainer.cs. Simply call
Congeal.exe with the sole argument a path to a preprocessed data file (see
the documentation in Util\TrainingDataPreprocessor for more information
about generating this file), and trained data will be produced in the output
directory under bin\Debug or bin\Release (depending on your
currently-selected build profile).

# -------------------------------------------------------------------------- #

Congealer originally by Jason Fennell, rewritten by James Brown.
