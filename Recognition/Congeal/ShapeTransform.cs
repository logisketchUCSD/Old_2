using System;
using System.Collections.Generic;
using System.Text;
using Sketch;

namespace Congeal
{
	/// <summary>
	/// Like an ImageTransform, but manipulates the original shape instead of the bitmap.
	/// </summary>
	class ShapeTransform
	{
		#region Internals

		private int _width, _height;
		private Shape _s;
		private CongealParameters _p;
		private CongealParameters _oldP;
		private CongealParameters _lastAppliedCP = null;
		private SymbolRec.Image.Image _latent;
		//private double[] _centroid = null;
		private bool _last_was_applied = false;
		private string _id;

		#endregion

		#region Constructors

		/// <summary>
		/// Create a new ShapeTransform
		/// </summary>
		/// <param name="s"></param>
		/// <param name="width"></param>
		/// <param name="height"></param>
		public ShapeTransform(Shape s, int width, int height, string id)
		{
			_s = s;
			_width = width;
			_height = height;
			_p = new CongealParameters(_height, _width);
			_oldP = new CongealParameters(_p);
			_latent = new SymbolRec.Image.Image(_width, _height, _s);
			_id = id;
		}

		#endregion

		#region Accessors

		/// <summary>
		/// The width of the rasterization of this ShapeTransform
		/// </summary>
		public int Width { get { return _width; } }

		/// <summary>
		/// The height of the rasterization of this ShapeTransform
		/// </summary>
		public int Height { get { return _height; } }

		/// <summary>
		/// The vector of congealing parameters applied to this ShapeTransform
		/// </summary>
		internal CongealParameters CPs { get { return new CongealParameters(_p); } }

		/// <summary>
		/// The latent image of this ShapeTransform
		/// </summary>
		public SymbolRec.Image.Image LatentImage
		{
			get
			{
				return _latent;
			}
		}

		/// <summary>
		/// The warp of this ShapeTransfrom
		/// </summary>
		public Warp Warp
		{
			get
			{
				return _p.getWarp(Centroid[0], Centroid[1]);
			}
		}

		/// <summary>
		/// The last set of CongealParameters that somebody tried to apply to this IT
		/// </summary>
		public CongealParameters LastCP
		{
			get
			{
				if (_lastAppliedCP == null)
					return new CongealParameters(_height, _width);
				return _lastAppliedCP;
			}
		}

		/// <summary>
		/// Whether or not we kept the last CP we tried to apply
		/// </summary>
		public bool LastCPWasApplied
		{
			get
			{
				return _last_was_applied;
			}
		}

		/// <summary>
		/// The centroid of the transformed shape
		/// </summary>
		public double[] Centroid { 
			get 
			{
				return _s.Centroid;
			} 
		}

		#endregion

		#region Public Methods

		/// <summary>
		/// Transform this image with new congeal parameters. By default, these
		/// modify the original parameters instead of replacing them.
		/// </summary>
		/// <param name="p"></param>
		public void append(CongealParameters newp)
		{
			_lastAppliedCP = newp;
			_oldP = new CongealParameters(_p);
			_p.append(newp);
			Shape newS = _p.getWarp(Centroid[0], Centroid[1]).warpShape(_s);
			_latent = new SymbolRec.Image.Image(_width, _height, newS);
			_last_was_applied = true;
		}

		/// <summary>
		/// Append and do not make undoable. In fact, change the ORIGINAL IMAGE!
		/// </summary>
		/// <param name="bp"></param>
		internal void noundo_append(CongealParameters bp)
		{
			_s = bp.getWarp(Centroid[0], Centroid[1]).warpShape(_s);
			_latent = new SymbolRec.Image.Image(_width, _height, _s);
		}

		/// <summary>
		/// Undo the last CongealParameters append
		/// </summary>
		public void undoCPAppend()
		{
			_latent = new SymbolRec.Image.Image(_width, _height, _oldP.getWarp(Centroid[0], Centroid[1]).warpShape(_s));
			_p = new CongealParameters(_oldP);
			_last_was_applied = false;
		}

		#endregion
	}
}
