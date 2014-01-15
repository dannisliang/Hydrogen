﻿#region Copyright Notice & License Information
//
// MeshCombiner.cs
//
// Author:
//       Matthew Davey <matthew.davey@dotbunny.com>
//       Robin Southern <betajaen@ihoed.com>
//
// Copyright (c) 2014 dotBunny Inc. (http://www.dotbunny.com)
//
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.
#endregion

using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace Hydrogen.Threading.Jobs
{
		public class MeshCombiner : ThreadPoolJob
		{
				/// <summary>
				/// Reference to Action to be used when ThreadedFunction is completed, however it requires that
				/// the Check method be called by Unity's main thread periodically. This will simply passback the 
				/// MeshDescriptions which then can be processed vai a coroutine/etc.
				/// </summary>
				Action<int, MeshDescription[], Dictionary<int, UnityEngine.Material>> _callback;
				/// <summary>
				/// An array of MeshDescriptions generated by the Combine method. 
				/// </summary>
				MeshDescription[] _combinedDescriptions = new MeshDescription[0];
				/// <summary>
				/// A dictionary of Materials referenced by the added meshes.
				/// </summary>
				Dictionary <int, UnityEngine.Material> _materialLookup = new Dictionary<int, UnityEngine.Material> ();
				/// <summary>
				/// An internal hash used to identify the Combine method instance.
				/// </summary>
				int _hash;
				/// <summary>
				/// An internally used datastore housing added mesh data to be combined by the Combine method.
				/// </summary>
				MeshDescription[] _meshDescriptions = new MeshDescription[0];

				/// <summary>
				/// Gets the array of MeshDescriptions generated by the Combine method. 
				/// </summary>
				/// <value>The the combined MeshDescritions.</value>
				public MeshDescription[] CombinedDescriptions {
						get { return _combinedDescriptions; }
				}

				/// <summary>
				/// Material reference .
				/// </summary>
				/// <value>The combined Materials.</value>
				public Dictionary<int, UnityEngine.Material> MaterialsLookup {
						get { return _materialLookup; }
				}

				/// <summary>
				/// Creates a thread safe dataset used when combining meshes via the MeshCombiner.
				/// </summary>
				/// <returns>The parsed MeshDescription.</returns>
				/// <param name="mesh">Target Mesh.</param>
				/// <param name="transform">Target Transform.</param>
				public static MeshDescription CreateDescription (UnityEngine.Mesh mesh, UnityEngine.Material[] material, Transform transform)
				{
						// Vertex Count - Don't want to hit Unity's properties over and over.
						var vertexCount = mesh.vertexCount;

						// Create our new MeshDescription based on the provided vertex count.
						var meshDescripion = new MeshDescription (vertexCount);

						// Copy information from the Unity Mesh into our MeshDescription
						meshDescripion.VertexObject.Vertices.CopyFrom (mesh.vertices);
						meshDescripion.VertexObject.Normals.CopyFrom (mesh.normals);
						meshDescripion.VertexObject.Tangents.CopyFrom (mesh.tangents);
						meshDescripion.VertexObject.Colors.CopyFrom (mesh.colors);
						meshDescripion.VertexObject.UV.CopyFrom (mesh.uv);
						meshDescripion.VertexObject.UV1.CopyFrom (mesh.uv1);
						meshDescripion.VertexObject.UV2.CopyFrom (mesh.uv2);
						meshDescripion.VertexObject.WorldTransform = transform.localToWorldMatrix;
					
						// SubMesh Count - Don't want to hit Unity's properties over and over.
						var subMeshCount = mesh.subMeshCount;

						// Iterate over the SubMeshes to be added to the description
						for (var j = 0; j < subMeshCount; j++) {
						
								var indices = mesh.GetIndices (j);

								// If the meshes dont have materials defined, use the last one
								int materialID = j >= material.Length ? material.Length - 1 : j;

								// Assign SubMesh
								var subMesh = meshDescripion.AddSubMesh (material [materialID].GetDataHashCode (), indices.Length);

								// TODO: This might be whats causing the issue when we add off indices



								subMesh.Indices.CopyFrom (indices);
						} 

						return meshDescripion;
				}

				/// <summary>
				/// Creates the thread safe dataset used when combining meshes via the MeshCombiner.
				/// </summary>
				/// <returns>The parsed MeshDescriptions.</returns>
				/// <param name="meshes">Target Mesh.</param>
				/// <param name = "materials"></param>
				/// <param name="transforms">Target Transform.</param>
				public static MeshDescription[] CreateDescriptions (UnityEngine.Mesh[] meshes, UnityEngine.Material[][] materials, Transform[] transforms)
				{
						// Create our holder
						var meshDescriptions = new MeshDescription[meshes.Length];

						// Lazy way of making a whole bunch.
						for (int i = 0; i < meshes.Length; i++) {
								meshDescriptions [i] = CreateDescription (meshes [i], materials [i], transforms [i]);
						}

						// Send it back!
						return meshDescriptions;
				}

				public static UnityEngine.Mesh CreateMesh (MeshDescription meshDescription)
				{
						var mesh = new UnityEngine.Mesh ();
						mesh.name = "MeshCombiner_" + UnityEngine.Random.Range (0, 1000);

						// A whole whack of data assignment.
						mesh.vertices = meshDescription.VertexObject.Vertices.ToArray ();

						mesh.normals = meshDescription.VertexObject.Normals.ToArray ();
						mesh.tangents = meshDescription.VertexObject.Tangents.ToArray ();
						mesh.colors = meshDescription.VertexObject.Colors.ToArray ();
						mesh.uv = meshDescription.VertexObject.UV.ToArray ();
						mesh.uv1 = meshDescription.VertexObject.UV1.ToArray ();
						mesh.uv2 = meshDescription.VertexObject.UV2.ToArray ();

						// Establish SubMesh Count (Should realistically be 0 right?)
						mesh.subMeshCount = meshDescription.SubMeshes.Count;

						// Itterate over the SubMeshes and assign their indices to the newly created Mesh.
						for (int y = 0; y < meshDescription.SubMeshes.Count; y++) {
						
								if (meshDescription.Topology == MeshTopology.Triangles) {
										mesh.SetIndices (
												meshDescription.SubMeshes [y].Indices.ToArray (), 
												MeshTopology.Triangles,
												y);
								} else {
										Debug.Log ("Non Triangles topology is currently not supported by the MeshCombiner.");
								}
						}
						return mesh;
				}

				public bool AddMaterial (UnityEngine.Material material)
				{
						// Cache our generating of the lookup code.
						int check = material.GetDataHashCode ();

						// Check if we have an entry already, and if we do not add it
						if (!_materialLookup.ContainsKey (check)) {
								_materialLookup.Add (check, material);
								return true;
						}

						return  false;
				}

				/// <summary>
				/// Add a Mesh to be combined.
				/// </summary>
				/// <returns><c>true</c>, if mesh was added, <c>false</c> otherwise.</returns>
				/// <param name="meshDescription">The MeshDescription to be added.</param>
				public bool AddMesh (MeshDescription meshDescription)
				{
						return Array.Add<MeshDescription> (ref _meshDescriptions, meshDescription);
				}

				/// <summary>
				/// Add a Mesh to be combined.
				/// </summary>
				/// <returns><c>true</c>, if mesh was added, <c>false</c> otherwise.</returns>
				/// <param name="mesh">The Mesh to be added.</param>
				/// <param name="transform">The Mesh's transform.</param>
				public bool AddMesh (UnityEngine.Mesh mesh, UnityEngine.Material[] materials, Transform transform)
				{
						return Array.Add<MeshDescription> (ref _meshDescriptions, CreateDescription (mesh, materials, transform));
				}

				public int Combine (Action<int, MeshDescription[], Dictionary<int, UnityEngine.Material>> onFinished)
				{
						return Combine (System.Threading.ThreadPriority.Normal, onFinished);
				}

				public int Combine (
						System.Threading.ThreadPriority priority, 
						Action<int, MeshDescription[], Dictionary<int, UnityEngine.Material>> onFinished)
				{
						// Generate Hash Code
						_hash = (Time.time + UnityEngine.Random.Range (0, 100)).GetHashCode ();

						// Start the threaded prcess
						if (onFinished != null) {
								_callback = onFinished;
						}

						Start (true, priority);

						return _hash;
				}

				public bool RemoveMaterial (UnityEngine.Material material)
				{
						int check = material.GetDataHashCode ();
						if (_materialLookup.ContainsKey (check)) {
								_materialLookup.Remove (check);
								return true;
						}
						return false;
				}

				public bool RemoveMesh (MeshDescription meshDescription)
				{
						return Array.Remove (ref _meshDescriptions, meshDescription);
				}
				//TODO: Add optimization to mesh data?
				protected sealed override void ThreadedFunction ()
				{
						// Create a temporary set of meshes based on the number of materials that are being used.
						var multiMeshDescriptions = new MultiMeshDescription[_materialLookup.Count];

						int materialCounter = 0;
						foreach (int i in _materialLookup.Keys) {
								multiMeshDescriptions [materialCounter] = new MultiMeshDescription (i);
								materialCounter++;
						}
								
						// Itterate through all of the MeshDescriptions present to be combined.
						foreach (var meshDescription in _meshDescriptions) {

								// Itterate through all SubMeshes in the MeshDescription
								foreach (var subMesh in meshDescription.SubMeshes) {
								
										// Itterate through all of our MultiMeshDescriptions adding meshes that
										// have the same material as its own.
										foreach (var multiMesh in multiMeshDescriptions) {

												if (multiMesh.SharedMaterial == subMesh.SharedMaterial) {
														multiMesh.AddSubMesh (subMesh);
														break;
												}
										}
								}
						}


						// Create our finalized MeshDescription holder, at the size of the materials
						var finalMeshDescriptions = new List<MeshDescription> (_materialLookup.Count);


						// Itterate over our MultiMeshDescriptions again and have them execute their 
						// Combine method forcing them down into MeshDescriptions.
						foreach (var multiMesh in multiMeshDescriptions) {
								finalMeshDescriptions.AddRange (multiMesh.Combine ());
						}

						// Assign the finalized MeshDescriptions to our internal storage.
						_combinedDescriptions = finalMeshDescriptions.ToArray ();
				}

				protected sealed override void OnFinished ()
				{
						// Callback
						if (_callback != null)
								_callback (_hash, _combinedDescriptions, _materialLookup);
				}

				public class VertexArrayDescription<T>
				{
						public readonly int Size;
						readonly T[] values;

						public bool HasValues { get; private set; }

						public VertexArrayDescription (int nbVertices)
						{
								Size = nbVertices;
								values = new T[Size];
								HasValues = false;
						}

						public T this [int i] {
								get { return values [i]; }
								set {
										values [i] = value;
										HasValues = true;
								}
						}

						public T[] ToArray ()
						{
								return Size == 0 ? null : values;
						}

						public void CopyFrom (T[] other)
						{
								if (other != null && other.Length > 0) {
										// TODO Exception/Assert here when size != values.length
										for (var i = 0; i < Size; i++) {
												values [i] = other [i];
										}
										HasValues = true;
								}
						}
				}

				public class IndexArrayDescription
				{
						public int Size { get; private set; }

						readonly int[] values;

						public IndexArrayDescription (int nbIndexes)
						{
								Size = nbIndexes;
								if (Size % 3 != 0) {

										Debug.Log ("Bad index array, count is not a multiple of 3! It is " + Size);
										return;
								}
								values = new int[Size];
						}

						public int[] ToArray ()
						{
								return Size == 0 ? null : values;
						}

						public int this [int i] {
								get { return values [i]; }
								set { values [i] = value; }
						}

						internal void CopyFrom (int[] other)
						{
								for (var i = 0; i < Size; i++) {
										values [i] = other [i];
								}
						}
				}

				public class MeshDescription
				{
						public MeshTopology Topology = MeshTopology.Triangles;
						public readonly List<SubMeshDescription> SubMeshes;
						public readonly VertexObjectDescription VertexObject;

						public MeshDescription (int verticesCount)
						{
								VertexObject = new VertexObjectDescription (verticesCount);
								SubMeshes = new List<SubMeshDescription> ();
						}

						public SubMeshDescription AddSubMesh (int sharedMaterial, int nbIndexes)
						{
								var smd = new SubMeshDescription (nbIndexes, VertexObject, sharedMaterial);
								SubMeshes.Add (smd);
								return smd;
						}

						internal void DebugPrint (StringBuilder sb)
						{
								sb.AppendFormat ("Mesh#{0:X8}\n", GetHashCode ());
								sb.AppendFormat ("Vertices.Size={0}\n", VertexObject.Size);
								sb.AppendFormat ("Vertices.Vertices =[{0},{1},{2}], [{3},{4},{5}], [{6},{7},{8}]...\n", VertexObject.Vertices [0].x, VertexObject.Vertices [1].y, VertexObject.Vertices [2].z, VertexObject.Vertices [3].x, VertexObject.Vertices [4].y, VertexObject.Vertices [5].z, VertexObject.Vertices [6].x, VertexObject.Vertices [7].y, VertexObject.Vertices [8].z);
								sb.AppendFormat ("Vertices.Normals={0}\n", VertexObject.Normals.HasValues);
								sb.AppendFormat ("Vertices.Tangents={0}\n", VertexObject.Tangents.HasValues);
								sb.AppendFormat ("Vertices.Colours={0}\n", VertexObject.Colors.HasValues);
								sb.AppendFormat ("Vertices.UV={0}\n", VertexObject.UV.HasValues);
								sb.AppendFormat ("Vertices.UV1={0}\n", VertexObject.UV1.HasValues);
								sb.AppendFormat ("Vertices.UV2={0}\n", VertexObject.UV2.HasValues);
								sb.AppendFormat ("Vertices.WorldTransform={0}\n", VertexObject.WorldTransform.ToString ().Replace ('\n', ' '));
								sb.AppendFormat ("SubMesh.Count={0}\n", SubMeshes.Count);
								for (var i = 0; i < SubMeshes.Count; i++) {
										var sm = SubMeshes [i];
										sb.AppendFormat ("SubMesh[{0}].Indexes={1}\n", i, sm.Indices.Size);
								}
						}
				}

				public class MultiMeshDescription
				{
						public readonly int SharedMaterial;
						public readonly List<SubMeshDescription> SubMeshes;

						public MultiMeshDescription (int material)
						{
								SharedMaterial = material;
								SubMeshes = new List<SubMeshDescription> (4);
						}

						public void AddSubMesh (SubMeshDescription sm)
						{
								SubMeshes.Add (sm);
						}

						class VertexElementQueue
						{
								public SubMeshDescription subMeshReference;
								public int targetMeshDescriptionIndex;
								public int targetIndexBegin;
								public int sourceIndexBegin;
								public int sourceIndexEnd;
						}

						public MeshDescription[] CombineNew ()
						{
								// Create our job queue
								var queue = new List<VertexElementQueue> ();

								// Create our target list to write too
								var newMeshes = new List<MeshDescription> ();

								var vertexCounts = new List<int> ();
								int vertexCount = 0x0000000;

								VertexElementQueue queued = null;
								int currentMeshDescriptionIndex = 0x0000000;
								int targetIndex = 0x00000000;


								// Cache used vertices count.
								foreach (var subMesh in SubMeshes) {
										if (queued == null) {
												queued = new VertexElementQueue ();
												queued.subMeshReference = subMesh;
												queued.targetMeshDescriptionIndex = currentMeshDescriptionIndex;

										}
										vertexCount += subMesh.CountUsedVertices ();

										// TODO: Make sure -1 is appropriate
										if (vertexCount > (Mesh.VerticesLimit - 1)) {
												vertexCounts.Add (Mesh.VerticesLimit - 1);
												vertexCount -= (Mesh.VerticesLimit - 1);
										}
								}

								// Add last one / or only one if
								if (vertexCount > 0)
										vertexCounts.Add (vertexCount);





								MeshDescription workingMesh;
								int baseTargetIndex = 0x00000000;

								foreach (var subMesh in SubMeshes) {

								}



								return newMeshes.ToArray ();
						}

						public MeshDescription[] Combine ()
						{	

								// Determine the total number of vertices accross submeshes
								int TotalVerticesCount = 0;
								foreach (var subMesh in SubMeshes) {

										//TODO: Should used CountUsedVertices
										Debug.Log ("SubMesh.Indices.Size: " + subMesh.Indices.Size);
										Debug.Log ("SubMesh.CountUsed: " + subMesh.CountUsedVertices ());

										//TotalVerticesCount += subMesh.CountUsedVertices ();
										TotalVerticesCount += subMesh.Indices.Size;

								}


								// Divide up meshes equally with a maxium vertex limit
								var meshNumberOfVertices = new List<int> ();
								int verticesCounter = 0;

								// TODO : Revisiting


								while (verticesCounter < TotalVerticesCount) {

										// Subtracked used from total left
										int used = TotalVerticesCount - verticesCounter;

										// I have a feeling this is a problem maker
										// We subtract from the numerical limit as the used counts 0 as 1
										if (used > (Mesh.VerticesLimit - 1)) {
												used = Mesh.VerticesLimit - 1;
										}


										meshNumberOfVertices.Add (used);

										verticesCounter += used;
								}


								var newDescriptions = new MeshDescription[meshNumberOfVertices.Count];

								for (int i = 0; i < meshNumberOfVertices.Count; i++) {

										int vertexCount = meshNumberOfVertices [i]; 

										// Initialize MeshDescription with number of vertices
										newDescriptions [i] = new MeshDescription (vertexCount);

										// TODO: Not sure on the SharedMaterial ... to do with teh array we create on start.
										var subMeshDescription = newDescriptions [i].AddSubMesh (SharedMaterial, vertexCount);

										// Instant re-ordering of vertices
										for (int j = 0; j < vertexCount; j++) {
												subMeshDescription.Indices [j] = j;
										}
								}
										

								int meshDescriptionIndex = 0;

								MeshDescription newMeshDescription = newDescriptions [meshDescriptionIndex];
								VertexObjectDescription targetVertexObject = newMeshDescription.VertexObject;

								int vertexIndex = 0;


								foreach (var subMeshDescription in SubMeshes) {

									
										VertexObjectDescription sourceVertexObject = subMeshDescription.VertexObject;



										// Check if we need to switch to the next mesh
										if ((vertexIndex + subMeshDescription.Indices.Size) > (Mesh.VerticesLimit)) {
												meshDescriptionIndex++;
												newMeshDescription = newDescriptions [meshDescriptionIndex];
												targetVertexObject = newMeshDescription.VertexObject;
												vertexIndex = 0;
												Debug.Log ("-------- NEW MESH ---------");
										}

										// Vertex Copy
										int j = vertexIndex;

									

										// TODO : THIS IS CAUSING EXCEPTIONS ON MESHES THAT REQUIRE SPLITTING
										// Something is definately a foul with the targetVertex or maybe the subMeshDescription.Indicies.Size
										try {
												for (int i = 0; i < subMeshDescription.Indices.Size; i++) {
												

														// Copy the index
														int index = subMeshDescription.Indices [i];

														// the issue is j in Vertices (tho the numbers are the same)
														if (j < targetVertexObject.Vertices.Size) {
																targetVertexObject.Vertices [j] = sourceVertexObject.WorldTransform.MultiplyPoint (
																		sourceVertexObject.Vertices [index]);
														} else {
																Debug.Log ("TOO BIG: " + j);
														}
														j++;
												}
										} catch (Exception e) {

												Console.WriteLine ("-------------- EXCEPTION --------------");
												Console.WriteLine (DateTime.Now.ToString ());
												Console.WriteLine ("targetVertexObject.Vertices.Size: " + targetVertexObject.Vertices.Size);
												Console.WriteLine ("j:" + j);


												//Array index is out of range.
												Console.WriteLine (e.GetBaseException ().Message);
												Console.WriteLine (e.GetBaseException ().InnerException);
												Console.WriteLine (e.GetBaseException ().StackTrace);
										}



										// Normal Copy
										if (sourceVertexObject.Normals.Size != 0) {
												j = vertexIndex;
												var inversedTransposedMatrix = sourceVertexObject.WorldTransform.inverse.transpose; 
												for (int i = 0; i < subMeshDescription.Indices.Size; i++) {
														// Copy the index
														int index = subMeshDescription.Indices [i];
														if (j < targetVertexObject.Normals.Size) {
																targetVertexObject.Normals [j] = 
																inversedTransposedMatrix.MultiplyVector (
																		sourceVertexObject.Normals [index]).normalized;
														}
														j++;
												}
										}

										// Tangents Copy
										if (sourceVertexObject.Tangents.Size != 0) {
												j = vertexIndex;
												var inversedTransposedMatrix = sourceVertexObject.WorldTransform.inverse.transpose; 
												for (int i = 0; i < subMeshDescription.Indices.Size; i++) {
														if (j < targetVertexObject.Tangents.Size) {
																// Copy the index
																int index = subMeshDescription.Indices [i];
																var p = sourceVertexObject.Tangents [index];
																var w = p.w;
																p = inversedTransposedMatrix.MultiplyVector (p);
																targetVertexObject.Tangents [j] = new Vector4 (p.x, p.y, p.z, w);

														}
														j++;
												}
										}

										// Colors Copy
										if (sourceVertexObject.Tangents.Size != 0) {
												j = vertexIndex;
												for (int i = 0; i < subMeshDescription.Indices.Size; i++) {
														// Copy the index
														int index = subMeshDescription.Indices [i];
														if (j < targetVertexObject.Colors.Size) {
																targetVertexObject.Colors [j] = sourceVertexObject.Colors [index];
														}
														j++;
												}
										}

										// UV Copy
										if (sourceVertexObject.UV.Size != 0) {
												j = vertexIndex;
												for (int i = 0; i < subMeshDescription.Indices.Size; i++) {
														// Copy the index
														int index = subMeshDescription.Indices [i];
														if (j < targetVertexObject.UV.Size) {
																targetVertexObject.UV [j] = sourceVertexObject.UV [index];
														}
														j++;
												}
										}
												
										// UV1 Copy
										if (sourceVertexObject.UV1.Size != 0) {
												j = vertexIndex;
												for (int i = 0; i < subMeshDescription.Indices.Size; i++) {
														// Copy the index
														int index = subMeshDescription.Indices [i];
														if (j < targetVertexObject.UV1.Size) {
																targetVertexObject.UV1 [j] = sourceVertexObject.UV1 [index];
														}
														j++;
												}
										}
												
										// UV2 Copy
										if (sourceVertexObject.UV.Size != 0) {
												j = vertexIndex;
												for (int i = 0; i < subMeshDescription.Indices.Size; i++) {
														// Copy the index
														int index = subMeshDescription.Indices [i];

														if (j < targetVertexObject.UV2.Size) {
																targetVertexObject.UV2 [j] = sourceVertexObject.UV2 [index];
														}
														j++;
												}
										}

										// Increase Index

										vertexIndex = j;

								}
								// Return our MeshDescriptions
								return newDescriptions;
						}
				}

				public class SubMeshDescription
				{
						public readonly IndexArrayDescription Indices;
						public readonly int SharedMaterial;
						public readonly int[] Used;
						public readonly VertexObjectDescription VertexObject;
						public int NumberOfVerticesUsed;

						public SubMeshDescription (int indexCount, VertexObjectDescription vertices, int material)
						{
								SharedMaterial = material;
								VertexObject = vertices;
								Indices = new IndexArrayDescription (indexCount);
								Used = new int[vertices.Size];
						}
						//TODO BROKEN
						public int CountUsedVertices ()
						{
								// This isnt accurate as its not used
								//return Indices.Size;

								NumberOfVerticesUsed = 0;

								// Count used vertices.  
								// This uses an 'if-less' solution so should be faster but 
								// uses 4x (int > bool) memory do to so.
								for (int i = 0; i < Used.Length; i++)
										Used [i] = 0;

								// Indices.Size
								for (int i = 0; i < Indices.Size; i++)
										Used [Indices [i]] = 1;

								for (int i = 0; i < Used.Length; i++) {
										NumberOfVerticesUsed += Used [i];
								}
							

								return NumberOfVerticesUsed;
						}
				}

				public class VertexObjectDescription
				{
						public readonly VertexArrayDescription<Color> Colors;
						public readonly VertexArrayDescription<Vector3> Normals;
						public readonly int Size;
						public readonly VertexArrayDescription<Vector4> Tangents;
						public readonly VertexArrayDescription<Vector2> UV;
						public readonly VertexArrayDescription<Vector2> UV1;
						public readonly VertexArrayDescription<Vector2> UV2;
						public readonly VertexArrayDescription<bool> Used;
						public readonly VertexArrayDescription<Vector3> Vertices;
						public Matrix4x4 WorldTransform;

						public VertexObjectDescription (int verticesCount)
						{
								Size = verticesCount;
								Vertices = new VertexArrayDescription<Vector3> (Size);
								Normals = new VertexArrayDescription<Vector3> (Size);
								Tangents = new VertexArrayDescription<Vector4> (Size);
								Colors = new VertexArrayDescription<Color> (Size);
								UV = new VertexArrayDescription<Vector2> (Size);
								UV1 = new VertexArrayDescription<Vector2> (Size);
								UV2 = new VertexArrayDescription<Vector2> (Size);
								Used = new VertexArrayDescription<bool> (Size);
								WorldTransform = Matrix4x4.identity;
						}
				}
		}
}
