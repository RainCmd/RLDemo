using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class MapBlockRenderer : IDisposable
{
    private const float blockWidth = ConfigMapBlockInfo.width - 1;
    private const float blockHeight = ConfigMapBlockInfo.height - 1;
    public struct MeshInfo
    {
        public Material material;
        public Mesh mesh;
        public MeshInfo(Material material, Mesh mesh)
        {
            this.material = material;
            this.mesh = mesh;
        }
    }
    public class BlockInfo
    {
        public Matrix4x4 matrix;
        public readonly List<MeshInfo> meshes = new List<MeshInfo>();
        public void Init(Vector3 position, ConfigMapBlockInfo info, Material[] materials)
        {
            matrix = Matrix4x4.TRS(position, Quaternion.identity, Vector3.one);
            var splats = Config.SplatInfos;
            for (var x = 0; x < ConfigMapBlockInfo.width; x++)
                for (var y = 0; y < ConfigMapBlockInfo.height; y++)
                {
                    var splat = info.GetSplat(x, y);
                    if (!splatIdxs.Contains(splat))
                        splatIdxs.Add(splat);
                }
            var vhs = new VertexHelper[splatIdxs.Count];
            for (var i = 0; i < vhs.Length; i++)
                vhs[i] = new VertexHelper();

            for (var x = 0; x < ConfigMapBlockInfo.width - 1; x++)
                for (var y = 0; y < ConfigMapBlockInfo.height - 1; y++)
                {
                    var splat00 = info.GetSplat(x, y);
                    var splat01 = info.GetSplat(x, y + 1);
                    var splat11 = info.GetSplat(x + 1, y + 1);
                    var splat10 = info.GetSplat(x + 1, y);
                    quad[0].position = new Vector3(x, y, 0);
                    quad[1].position = new Vector3(x + 1, y, 0);
                    quad[2].position = new Vector3(x + 1, y + 1, 0);
                    quad[3].position = new Vector3(x, y + 1, 0);
                    //00
                    var uv = new Vector2(.5f, .75f);
                    if (splat00 == splat01) uv.x += .25f;
                    if (splat00 == splat10) uv.y -= .5f;
                    if (splat00 == splat11) uv.y -= .25f;
                    if (splats[splat00].extend)
                    {
                        if (splat00 == splat10 && splat00 == splat01 && splat00 == splat11)
                        {
                            var extend = info.GetExtend(x, y);
                            uv = new Vector2(extend & 3, extend >> 2) * .25f;
                            uv.x += 1;
                        }
                        SetUV(uv, .5f);
                    }
                    else SetUV(uv, 1);
                    vhs[splatIdxs.IndexOf(splat00)].AddUIVertexQuad(quad);

                    //01
                    if (splat00 != splat01)
                    {
                        uv = new Vector2(0, .25f);
                        if (splat01 == splat10) uv.x += .25f;
                        if (splat01 == splat11) uv.y -= .25f;
                        SetUV(uv, splats[splat01].extend ? .5f : 1f);
                        vhs[splatIdxs.IndexOf(splat01)].AddUIVertexQuad(quad);
                    }
                    //11
                    if (splat00 != splat11 && splat01 != splat11)
                    {
                        uv = new Vector2(0, .5f);
                        if (splat10 == splat11) uv.x += .25f;
                        SetUV(uv, splats[splat11].extend ? .5f : 1f);
                        vhs[splatIdxs.IndexOf(splat11)].AddUIVertexQuad(quad);
                    }
                    //10
                    if (splat00 != splat10 && splat01 != splat10 && splat11 != splat10)
                    {
                        uv = new Vector2(.25f, .75f);
                        SetUV(uv, splats[splat10].extend ? .5f : 1f);
                        vhs[splatIdxs.IndexOf(splat10)].AddUIVertexQuad(quad);
                    }
                }

            for (var i = 0; i < vhs.Length; i++)
            {
                var mesh = meshPool.Count > 0 ? meshPool.Pop() : new Mesh();
                mesh.name = string.Format("MapBlock pos:{0} splat:{1}", position, splatIdxs[i]);
                vhs[i].FillMesh(mesh);
                meshes.Add(new MeshInfo(materials[splatIdxs[i]], mesh));
                vhs[i].Dispose();
            }
            splatIdxs.Clear();
        }
        public void Recycle()
        {
            foreach (var mesh in meshes) meshPool.Push(mesh.mesh);
            meshes.Clear();
        }
        public void Draw()
        {
            foreach (var mesh in meshes)
                Graphics.DrawMesh(mesh.mesh, matrix, mesh.material, 0);
        }
        private static void SetUV(Vector2 uv, float scale)
        {
            uv.x *= scale;
            var size = new Vector2(.25f * scale, .25f);
            quad[0].uv0 = new Vector2(uv.x, uv.y);
            quad[1].uv0 = new Vector2(uv.x + size.x, uv.y);
            quad[2].uv0 = new Vector2(uv.x + size.x, uv.y + size.y);
            quad[3].uv0 = new Vector2(uv.x, uv.y + size.y);
        }
        private static readonly UIVertex[] quad = new UIVertex[4];
        private static readonly List<int> splatIdxs = new List<int>();
    }
    private readonly CameraMgr mgr;
    private readonly Material[] materials;
    private readonly BlockInfo[,] blocks;
    private int lastMinX = 0, lastMinY = 0, lastMaxX = 0, lastMaxY = 0;
    public MapBlockRenderer(CameraMgr mgr, LoadingProgress loading)
    {
        this.mgr = mgr;
        var blocks = Config.MapBlocks;
        this.blocks = new BlockInfo[blocks.width, blocks.height];
        var splats = Config.SplatInfos;
        materials = new Material[splats.Count];
        for (int i = 0; i < splats.Count; i++)
        {
            var mat = new Material(Shader.Find("Unlit/Splat"));
            mat.SetTexture("_MainTex", splats[i].splat);
            mat.renderQueue = 1000 + i;
            materials[i] = mat;
            loading.Progress = (float)i / splats.Count;
        }
        loading.Progress = 1;
    }
    public void OnRendererUpdate()
    {
        return;
        var area = mgr.CameraArea;
        var blocks = Config.MapBlocks;
        var minX = Mathf.Clamp((int)(area.min.x / blockWidth), 0, blocks.width - 1);
        var maxX = Mathf.Clamp((int)(area.max.x / blockWidth), 0, blocks.width - 1);
        var minY = Mathf.Clamp((int)(area.min.y / blockHeight), 0, blocks.height - 1);
        var maxY = Mathf.Clamp((int)(area.max.y / blockHeight), 0, blocks.height - 1);
        if (lastMinX < minX || lastMinY < minY || lastMaxX > maxX || lastMaxY > maxY)
            for (var x = lastMinX; x <= lastMaxX; x++)
                for (var y = lastMinY; y <= lastMaxY; y++)
                    if ((lastMinX > maxX || lastMaxX < minX || lastMinY > maxY || lastMaxY < minY) && this.blocks[x, y] != null)
                    {
                        this.blocks[x, y].Recycle();
                        blockPool.Push(this.blocks[x, y]);
                        this.blocks[x, y] = null;
                    }
        for (var x = minX; x <= maxX; x++)
            for (var y = minY; y <= maxY; y++)
            {
                if (this.blocks[x, y] == null)
                {
                    this.blocks[x, y] = blockPool.Count > 0 ? blockPool.Pop() : new BlockInfo();
                    this.blocks[x, y].Init(new Vector3(x * blockWidth, y * blockHeight), blocks[x, y], materials);
                }
                this.blocks[x, y].Draw();
            }
        lastMinX = minX; lastMaxX = maxX;
        lastMinY = minY; lastMaxY = maxY;
    }

    public void Dispose()
    {
        return;
        foreach (var mat in materials) UnityEngine.Object.DestroyImmediate(mat);
        for (var x = lastMinX; x <= lastMaxX; x++)
            for (var y = lastMinY; y <= lastMaxY; y++)
                if (blocks[x, y] != null)
                {
                    blocks[x, y].Recycle();
                    blockPool.Push(blocks[x, y]);
                    blocks[x, y] = null;
                }
    }

    private static readonly Stack<BlockInfo> blockPool = new Stack<BlockInfo>();
    private static readonly Stack<Mesh> meshPool = new Stack<Mesh>();
}
