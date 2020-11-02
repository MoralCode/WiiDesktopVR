//--------------------------------------------------------------------------------------
// File: DXMUTMesh.cs
//
// Support code for loading DirectX .X files.
//
// Copyright (c) Microsoft Corporation. All rights reserved.
//--------------------------------------------------------------------------------------

using System;
using System.Diagnostics;
using System.IO;
using SharpDX;
using SharpDX.Direct3D9;

namespace Microsoft.Samples.DirectX.UtilityToolkit
{
    /// <summary>Class for loading and rendering file-based meshes</summary>
    public sealed class FrameworkMesh : IDisposable
    {
        #region Instance Data

        private readonly string meshFileName;
        private Mesh systemMemoryMesh = null; // System Memory mesh, lives through a resize
        private Mesh localMemoryMesh = null; // Local mesh, rebuilt on resize

        private Material[] meshMaterials; // Materials for the mesh
        private BaseTexture[] meshTexture; // Texture for the mesh

        /// <summary>Returns the system memory mesh</summary>
        public Mesh SystemMesh => systemMemoryMesh;

        /// <summary>Returns the local memory mesh</summary>
        public Mesh LocalMesh => localMemoryMesh;

        /// <summary>Should the mesh be rendered with materials</summary>
        public bool IsUsingMaterials { get; set; } = true;

        /// <summary>Number of materials in mesh</summary>
        public int NumberMaterials => meshMaterials.Length;

        /// <summary>Gets a texture from the mesh</summary>
        public BaseTexture GetTexture(int index)
        {
            return meshTexture[index];
        }

        /// <summary>Gets a material from the mesh</summary>
        public Material GetMaterial(int index)
        {
            return meshMaterials[index];
        }

        #endregion

        #region Creation

        /// <summary>Create a new mesh using this file</summary>
        public FrameworkMesh(Device device, string name)
        {
            meshFileName = name;
            Create(device, meshFileName);
        }

        /// <summary>Create a new mesh</summary>
        public FrameworkMesh() : this(null, "FrameworkMeshFile_Mesh")
        {
        }

        /// <summary>Create the mesh data</summary>
        public void Create(Device device, string name)
        {
            // Hook the device events
            Debug.Assert(device != null, "Device should not be null.");
            device.DeviceLost += new EventHandler(OnLostDevice);
            device.DeviceReset += new EventHandler(OnResetDevice);
            device.Disposing += new EventHandler(OnDeviceDisposing);

            GraphicsStream adjacency; // Adjacency information
            ExtendedMaterial[] materials; // Mesh material information

            // First try to find the filename
            var path = string.Empty;
            try
            {
                path = Utility.FindMediaFile(name);
            }
            catch (MediaNotFoundException)
            {
                // The media was not found, maybe a full path was passed in?
                if (File.Exists(name))
                    path = name;
                else
                    // No idea what this is trying to find
                    throw new MediaNotFoundException();
            }

            // Now load the mesh
            systemMemoryMesh = Mesh.FromFile(path, MeshFlags.SystemMemory, device, out adjacency,
                out materials);

            using (adjacency)
            {
                // Optimize the mesh for performance
                systemMemoryMesh.OptimizeInplace(MeshFlags.OptimizeVertexCache | MeshFlags.OptimizeCompact |
                                                 MeshFlags.OptimizeAttributeSort, adjacency);

                // Find the folder of where the mesh file is located
                var folder = Utility.AppendDirectorySeparator(new FileInfo(path).DirectoryName);

                // Create the materials
                CreateMaterials(folder, device, adjacency, materials);
            }

            // Finally call reset
            OnResetDevice(device, EventArgs.Empty);
        }
        // TODO: Create with XOF

        /// <summary>Create the materials for the mesh</summary>
        public void CreateMaterials(string folder, Device device, GraphicsStream adjacency,
            ExtendedMaterial[] materials)
        {
            // Does the mesh have materials?
            if (materials != null && materials.Length > 0)
            {
                // Allocate the arrays for the materials
                meshMaterials = new Material[materials.Length];
                meshTexture = new BaseTexture[materials.Length];

                // Copy each material and create it's texture
                for (var i = 0; i < materials.Length; i++)
                {
                    // Copy the material first
                    meshMaterials[i] = materials[i].MaterialD3D;

                    // Is there a texture for this material?
                    if (materials[i].TextureFileName == null || materials[i].TextureFileName.Length == 0)
                        continue; // No, just continue now

                    ImageInformation info = new ImageInformation();
                    var textureFile = folder + materials[i].TextureFileName;
                    try
                    {
                        // First look for the texture in the same folder as the input folder
                        info = Texture.ImageInformationFromFile(textureFile);
                    }
                    catch
                    {
                        try
                        {
                            // Couldn't find it, look in the media folder
                            textureFile = Utility.FindMediaFile(materials[i].TextureFileName);
                            info = Texture.ImageInformationFromFile(textureFile);
                        }
                        catch (MediaNotFoundException)
                        {
                            // Couldn't find it anywhere, skip it
                            continue;
                        }
                    }

                    switch (info.ResourceType)
                    {
                        case ResourceType.Texture:
                            meshTexture[i] = Texture.FromFile(device, textureFile);
                            break;
                        case ResourceType.CubeTexture:
                            meshTexture[i] = Texture.FromCubeFile(device, textureFile);
                            break;
                        case ResourceType.VolumeTexture:
                            meshTexture[i] = Texture.FromVolumeFile(device, textureFile);
                            break;
                    }
                }
            }
        }

        #endregion

        #region Class Methods

        /// <summary>Updates the mesh to a new vertex format</summary>
        public void SetVertexFormat(Device device, VertexFormat format)
        {
            Mesh tempSystemMesh = null;
            Mesh tempLocalMesh = null;
            VertexFormat oldFormat = VertexFormat.None;
            using (systemMemoryMesh)
            {
                using (localMemoryMesh)
                {
                    // Clone the meshes
                    if (systemMemoryMesh != null)
                    {
                        oldFormat = systemMemoryMesh.VertexFormat;
                        tempSystemMesh = systemMemoryMesh.CloneMesh(systemMemoryMesh.Options.Value,
                            format, device);
                    }

                    if (localMemoryMesh != null)
                        tempLocalMesh = localMemoryMesh.CloneMesh(localMemoryMesh.Options.Value,
                            format, device);
                }
            }

            // Store the new meshes
            systemMemoryMesh = tempSystemMesh;
            localMemoryMesh = tempLocalMesh;

            // Compute normals if they are being requested and the old mesh didn't have them
            if ((oldFormat & VertexFormat.Normal) == 0 && format != 0)
            {
                if (systemMemoryMesh != null)
                    systemMemoryMesh.ComputeNormals();
                if (localMemoryMesh != null)
                    localMemoryMesh.ComputeNormals();
            }
        }

        /// <summary>Updates the mesh to a new vertex declaration</summary>
        public void SetVertexDeclaration(Device device, VertexElement[] decl)
        {
            Mesh tempSystemMesh = null;
            Mesh tempLocalMesh = null;
            VertexElement[] oldDecl = null;
            using (systemMemoryMesh)
            {
                using (localMemoryMesh)
                {
                    // Clone the meshes
                    if (systemMemoryMesh != null)
                    {
                        oldDecl = systemMemoryMesh.Declaration;
                        tempSystemMesh = systemMemoryMesh.CloneMesh(systemMemoryMesh.Options.Value,
                            decl, device);
                    }

                    if (localMemoryMesh != null)
                        tempLocalMesh = localMemoryMesh.CloneMesh(localMemoryMesh.Options.Value,
                            decl, device);
                }
            }

            // Store the new meshes
            systemMemoryMesh = tempSystemMesh;
            localMemoryMesh = tempLocalMesh;

            var hadNormal = false;
            // Check if the old declaration contains a normal.
            for (var i = 0; i < oldDecl.Length; i++)
                if (oldDecl[i].DeclarationUsage == DeclarationUsage.Normal)
                {
                    hadNormal = true;
                    break;
                }

            // Check to see if the new declaration has a normal
            var hasNormalNow = false;
            for (var i = 0; i < decl.Length; i++)
                if (decl[i].DeclarationUsage == DeclarationUsage.Normal)
                {
                    hasNormalNow = true;
                    break;
                }

            // Compute normals if they are being requested and the old mesh didn't have them
            if (!hadNormal && hasNormalNow)
            {
                if (systemMemoryMesh != null)
                    systemMemoryMesh.ComputeNormals();
                if (localMemoryMesh != null)
                    localMemoryMesh.ComputeNormals();
            }
        }

        /// <summary>Occurs after the device has been reset</summary>
        private void OnResetDevice(object sender, EventArgs e)
        {
            Device device = sender as Device;
            if (systemMemoryMesh == null)
                throw new InvalidOperationException("There is no system memory mesh.  Nothing to do here.");

            // Make a local memory version of the mesh. Note: because we are passing in
            // no flags, the default behavior is to clone into local memory.
            localMemoryMesh = systemMemoryMesh.CloneMesh(systemMemoryMesh.Options.Value & ~MeshFlags.SystemMemory,
                systemMemoryMesh.VertexFormat, device);
        }

        /// <summary>Occurs before the device is going to be reset</summary>
        private void OnLostDevice(object sender, EventArgs e)
        {
            if (localMemoryMesh != null)
                localMemoryMesh.Dispose();

            localMemoryMesh = null;
        }

        /// <summary>Renders this mesh</summary>
        public void Render(Device device, bool canDrawOpaque, bool canDrawAlpha)
        {
            if (localMemoryMesh == null)
                throw new InvalidOperationException("No local memory mesh.");

            // Frist, draw the subsets without alpha
            if (canDrawOpaque)
                for (var i = 0; i < meshMaterials.Length; i++)
                {
                    if (IsUsingMaterials)
                    {
                        if (meshMaterials[i].DiffuseColor.Alpha < 1.0f)
                            continue; // Only drawing opaque right now

                        // set the device material and texture
                        device.Material = meshMaterials[i];
                        device.SetTexture(0, meshTexture[i]);
                    }

                    localMemoryMesh.DrawSubset(i);
                }

            // Then, draw the subsets with alpha
            if (canDrawAlpha)
                for (var i = 0; i < meshMaterials.Length; i++)
                {
                    if (meshMaterials[i].DiffuseColor.Alpha == 1.0f)
                        continue; // Only drawing non-opaque right now

                    // set the device material and texture
                    device.Material = meshMaterials[i];
                    device.SetTexture(0, meshTexture[i]);
                    localMemoryMesh.DrawSubset(i);
                }
        }

        /// <summary>Renders this mesh</summary>
        public void Render(Device device)
        {
            Render(device, true, true);
        }

        // TODO: Render with effect

        /// <summary>Compute a bounding sphere for this mesh</summary>
        public float ComputeBoundingSphere(out Vector3 center)
        {
            if (systemMemoryMesh == null)
                throw new InvalidOperationException("There is no system memory mesh.  Nothing to do here.");

            // Get the object declaration
            int strideSize = VertexInformation.GetFormatSize(systemMemoryMesh.VertexFormat);

            // Lock the vertex buffer
            GraphicsStream data = null;
            try
            {
                data = systemMemoryMesh.LockVertexBuffer(LockFlags.ReadOnly);
                // Now compute the bounding sphere
                return Geometry.ComputeBoundingSphere(data, systemMemoryMesh.NumberVertices,
                    systemMemoryMesh.VertexFormat, out center);
            }
            finally
            {
                // Make sure to unlock the vertex buffer
                if (data != null)
                    systemMemoryMesh.UnlockVertexBuffer();
            }
        }

        #endregion

        #region IDisposable Members

        /// <summary>Cleans up any resources required when this object is disposed</summary>
        public void Dispose()
        {
            OnLostDevice(null, EventArgs.Empty);
            if (meshTexture != null)
                for (var i = 0; i < meshTexture.Length; i++)
                    if (meshTexture[i] != null)
                        meshTexture[i].Dispose();
            meshTexture = null;
            meshMaterials = null;

            if (systemMemoryMesh != null)
                systemMemoryMesh.Dispose();

            systemMemoryMesh = null;
        }

        /// <summary>Cleans up any resources required when this object is disposed</summary>
        private void OnDeviceDisposing(object sender, EventArgs e)
        {
            // Just dispose of our class
            Dispose();
        }

        #endregion
    }
}