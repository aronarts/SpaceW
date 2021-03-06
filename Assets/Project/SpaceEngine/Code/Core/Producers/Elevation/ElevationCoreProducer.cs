﻿#region License
// Procedural planet generator.
//  
// Copyright (C) 2015-2017 Denis Ovchinnikov [zameran] 
// All rights reserved.
// 
// Redistribution and use in source and binary forms, with or without
// modification, are permitted provided that the following conditions
// are met:
// 1. Redistributions of source code must retain the above copyright
//     notice, this list of conditions and the following disclaimer.
// 2. Redistributions in binary form must reproduce the above copyright
//    notice, this list of conditions and the following disclaimer in the
//    documentation and/or other materials provided with the distribution.
// 3. Neither the name of the copyright holders nor the names of its
//    contributors may be used to endorse or promote products derived from
//    this software without specific prior written permission.
// 
// THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS "AS IS"
// AND ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE
// IMPLIED WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE
// ARE DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT OWNER OR CONTRIBUTORS BE
// LIABLE FOR ANY DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR
// CONSEQUENTIAL DAMAGES (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF
// SUBSTITUTE GOODS OR SERVICES; LOSS OF USE, DATA, OR PROFITS; OR BUSINESS
// INTERRUPTION)HOWEVER CAUSED AND ON ANY THEORY OF LIABILITY, WHETHER IN
// CONTRACT, STRICT LIABILITY, OR TORT (INCLUDING NEGLIGENCE OR OTHERWISE)
// ARISING IN ANY WAY OUT OF THE USE OF THIS SOFTWARE, EVEN IF ADVISED OF
// THE POSSIBILITY OF SUCH DAMAGE.
// 
// Creation Date: 2017.02.23
// Creation Time: 4:46 PM
// Creator: zameran
#endregion

using SpaceEngine.Core.Bodies;
using SpaceEngine.Core.Exceptions;
using SpaceEngine.Core.Storage;
using SpaceEngine.Core.Terrain;
using SpaceEngine.Core.Tile.Producer;
using SpaceEngine.Core.Tile.Storage;

using System;
using System.Collections.Generic;

using UnityEngine;

namespace SpaceEngine.Core
{
    public class ElevationCoreProducer : TileProducer
    {
        public GameObject ResidualProducerGameObject;

        private TileProducer ResidualProducer;

        public Material ElevationMaterial;

        protected override void Start()
        {
            base.Start();

            if (TerrainNode == null) { TerrainNode = transform.parent.GetComponent<TerrainNode>(); }
            if (TerrainNode.ParentBody == null) { TerrainNode.ParentBody = transform.parent.GetComponentInParent<Body>(); }

            if (ResidualProducerGameObject != null)
            {
                if (ResidualProducer == null) { ResidualProducer = ResidualProducerGameObject.GetComponent<TileProducer>(); }
                if (ResidualProducer.Cache == null) { ResidualProducer.InitCache(); }
            }

            var tileSize = GetTileSize(0);

            if ((tileSize - GetBorder() * 2 - 1) % (TerrainNode.ParentBody.GridResolution - 1) != 0)
            {
                throw new InvalidParameterException("Tile size - border * 2 - 1 must be divisible by grid mesh resolution - 1" + string.Format(": {0}-{1}", tileSize, GetBorder()));
            }

            if (ResidualProducer != null)
            {
                if (ResidualProducer.GetTileSize(0) != tileSize) throw new InvalidParameterException("Residual tile size must match elevation tile size!");
                if (!(ResidualProducer.Cache.GetStorage(0) is GPUTileStorage)) throw new InvalidStorageException("Residual storage must be a GPUTileStorage");
            }

            var storage = Cache.GetStorage(0) as GPUTileStorage;

            if (storage == null)
            {
                throw new InvalidStorageException("Storage must be a GPUTileStorage");
            }
        }

        public override int GetBorder()
        {
            return 2;
        }

        public override void DoCreateTile(int level, int tx, int ty, List<TileStorage.Slot> slot)
        {
            var gpuSlot = slot[0] as GPUTileStorage.GPUSlot;

            if (gpuSlot == null) { throw new NullReferenceException("gpuSlot"); }

            var tileWidth = gpuSlot.Owner.TileSize;
            var tileSize = tileWidth - (1 + GetBorder() * 2);

            var rootQuadSize = TerrainNode.TerrainQuadRoot.Length;

            if (ResidualProducer != null)
            {
                if (ResidualProducer.HasTile(level, tx, ty))
                {
                    GPUTileStorage.GPUSlot residualGpuSlot = null;

                    var residualTile = ResidualProducer.FindTile(level, tx, ty, false, true);

                    if (residualTile != null)
                        residualGpuSlot = residualTile.GetSlot(0) as GPUTileStorage.GPUSlot;
                    else
                    { throw new MissingTileException("Find residual tile failed"); }

                    if (residualGpuSlot == null) { throw new MissingTileException("Find parent tile failed"); }

                    ElevationMaterial.SetTexture("_ResidualSampler", residualGpuSlot.Texture);
                    ElevationMaterial.SetVector("_ResidualOSH", new Vector4(0.25f / (float)tileWidth, 0.25f / (float)tileWidth, 2.0f / (float)tileWidth, 1.0f));
                }
                else
                {
                    ElevationMaterial.SetTexture("_ResidualSampler", null);
                    ElevationMaterial.SetVector("_ResidualOSH", new Vector4(0.0f, 0.0f, 1.0f, 0.0f));

                    Debug.LogError(string.Format("Residual producer exist, but can't find any suitable tile at {0}:{1}:{2}!", level, tx, ty));
                }
            }
            else
            {
                ElevationMaterial.SetTexture("_ResidualSampler", null);
                ElevationMaterial.SetVector("_ResidualOSH", new Vector4(0.0f, 0.0f, 1.0f, 0.0f));
            }

            var tileWSD = Vector4.zero;
            tileWSD.x = (float)tileWidth;
            tileWSD.y = (float)rootQuadSize / (float)(1 << level) / (float)tileSize;
            tileWSD.z = (float)tileSize / (float)(TerrainNode.ParentBody.GridResolution - 1);
            tileWSD.w = 0.0f;

            var tileSD = Vector2d.zero;
            tileSD.x = (0.5 + (float)GetBorder()) / (tileWSD.x - 1 - (float)GetBorder() * 2);
            tileSD.y = (1.0 + tileSD.x * 2.0);

            var offset = Vector4d.zero;
            offset.x = ((double)tx / (1 << level) - 0.5) * rootQuadSize;
            offset.y = ((double)ty / (1 << level) - 0.5) * rootQuadSize;
            offset.z = rootQuadSize / (1 << level);
            offset.w = TerrainNode.ParentBody.Size;

            ElevationMaterial.SetVector("_TileWSD", tileWSD);
            ElevationMaterial.SetVector("_TileSD", tileSD.ToVector2());
            ElevationMaterial.SetFloat("_Amplitude", TerrainNode.ParentBody.Amplitude);
            ElevationMaterial.SetFloat("_Frequency", TerrainNode.ParentBody.Frequency);
            ElevationMaterial.SetVector("_Offset", offset.ToVector4());
            ElevationMaterial.SetMatrix("_LocalToWorld", TerrainNode.FaceToLocal.ToMatrix4x4());

            if (TerrainNode.ParentBody.TCCPS != null) TerrainNode.ParentBody.TCCPS.SetUniforms(ElevationMaterial);

            Graphics.Blit(null, gpuSlot.Texture, ElevationMaterial);

            base.DoCreateTile(level, tx, ty, slot);
        }
    }
}