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
//    notice, this list of conditions and the following disclaimer.
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
// Creation Date: Undefined
// Creation Time: Undefined
// Creator: zameran
#endregion


using UnityEngine;

namespace SpaceEngine.Debugging
{
    public sealed class DebugDrawTerrainNode : DebugDraw
    {
        private readonly Color[] Colors = new Color[] { Color.blue, Color.red, Color.yellow, Color.green, Color.magenta, Color.cyan };

        protected override void Start()
        {
            base.Start();
        }

        protected override void OnPostRender()
        {
            base.OnPostRender();
        }

        protected override void CreateLineMaterial()
        {
            base.CreateLineMaterial();
        }

        protected override void Draw()
        {
#if UNITY_EDITOR
            if (UnityEditor.SceneView.currentDrawingSceneView != null) return; //Do not draw at Scene tab in editor.
#endif

            var target = GodManager.Instance.ActiveBody;

            if (target == null) return;

            for (byte i = 0; i < target.TerrainNodes.Count; i++)
            {
                var q = target.TerrainNodes[i];
                var root = q.TerrainQuadRoot;

                if (root == null) continue;

                root.DrawQuadOutline(CameraHelper.Main(), lineMaterial, Colors[q.Face % 6]);
            }
        }
    }
}