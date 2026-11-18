using Unity.Collections;
using Unity.Jobs;
using Unity.Burst;
using Unity.Mathematics;

namespace Grass
{
    [BurstCompile]
    public struct FrustumCulling
    {
        public static byte FrustumCullingBounds(HWorldBounds bounds, float4x4 fvp/*, float3 lightDir, bool shadowCulling = false*/)
        {
            byte isVisible = 0;
            
            // bound 包围盒中最大的 Pos 和最小的 Pos
            var minPos = bounds.Center - bounds.Extents;
            var maxPos = bounds.Center + bounds.Extents;
            
            // bound 包围盒的八个顶点，分别左乘 fvp矩阵，用来转换到 NDC空间（还没除以第四个分量投影变换）
            var a0 = math.mul(fvp, math.float4(minPos, 1));
            var a1 = math.mul(fvp, math.float4(minPos.xy, maxPos.z, 1));
            var a2 = math.mul(fvp, math.float4(minPos.x, maxPos.y, minPos.z, 1));
            var a3 = math.mul(fvp, math.float4(minPos.x, maxPos.yz, 1));
            var a4 = math.mul(fvp, math.float4(maxPos.x, minPos.yz, 1));
            var a5 = math.mul(fvp, math.float4(maxPos.x, minPos.y, maxPos.z, 1));
            var a6 = math.mul(fvp, math.float4(maxPos.xy, minPos.z, 1));
            var a7 = math.mul(fvp, math.float4(maxPos, 1));

            //if (IsVisibleAfterFrustumCulling(a0) ||
            //    IsVisibleAfterFrustumCulling(a1) ||
            //    IsVisibleAfterFrustumCulling(a2) ||
            //    IsVisibleAfterFrustumCulling(a3) ||
            //    IsVisibleAfterFrustumCulling(a4) ||
            //    IsVisibleAfterFrustumCulling(a5) ||
            //    IsVisibleAfterFrustumCulling(a6) ||
            //    IsVisibleAfterFrustumCulling(a7) ||
            //    IsContain(a0, a1, a2, a3, a4, a5, a6, a7))
            //{
            //    isVisible = 1;
            //}
            if (!IsNoContain(a0, a1, a2, a3, a4, a5, a6, a7))
            {
                isVisible = 1;

            }
            return isVisible;
        }

        public static bool IsContain(float4 a0, float4 a1, float4 a2, float4 a3, float4 a4, float4 a5, float4 a6, float4 a7)
        {
            float4x4 m1 = math.float4x4(a0, a1, a2, a3);
            float4x4 m2 = math.float4x4(a4, a5, a6, a7);

            var tm1 = math.transpose(m1);
            var tm2 = math.transpose(m2);

            return !((math.all(tm1.c0 < -tm1.c3) && math.all(tm2.c0 < -tm2.c3)) || (math.all(tm1.c0 > tm1.c3) && math.all(tm2.c0 > tm2.c3))) &&
                   !((math.all(tm1.c1 < -tm1.c3) && math.all(tm2.c1 < -tm2.c3)) || (math.all(tm1.c1 > tm1.c3) && math.all(tm2.c1 > tm2.c3))) &&
                   !((math.all(tm1.c2 < -tm1.c3) && math.all(tm2.c2 < -tm2.c3)) || (math.all(tm1.c2 > tm1.c3) && math.all(tm2.c2 > tm2.c3)));
        }

        public static bool IsNoContain(float4 a0, float4 a1, float4 a2, float4 a3, float4 a4, float4 a5, float4 a6, float4 a7)
        {
            float4x4 m1 = math.float4x4(a0, a1, a2, a3);
            float4x4 m2 = math.float4x4(a4, a5, a6, a7);

            // 矩阵转置 T
            var tm1 = math.transpose(m1);
            var tm2 = math.transpose(m2);
            
            // 转置之后，例如 tm1 的第一行 (a0.x, a0.y, a0.z, n) , a0表示左乘了投影矩阵之后的包围盒顶点，其中最后一个 n 是用来做透视除法的
            // 也就是透视除法之后，tm1 第一行变成了 (a0.x / n, a0.y / n , a0.z / n , 1)， 变到了NDC空间
            // 如果第一项 a0.x / n < -1 说明点在 NDC 空间之外，也就是需要裁剪掉，数学变换，也就是 a0.x < -n 也就是tm1.c0.x < tm1.c3.x。
            // math.all 表示如果4行全都在 NDC 空间之外，则需要裁剪。
            return (math.all(tm1.c0 < -tm1.c3) && math.all(tm2.c0 < -tm2.c3)) ||
                   (math.all(tm1.c0 > tm1.c3) && math.all(tm2.c0 > tm2.c3)) ||
                   (math.all(tm1.c1 < -tm1.c3) && math.all(tm2.c1 < -tm2.c3)) ||
                   (math.all(tm1.c1 > tm1.c3) && math.all(tm2.c1 > tm2.c3)) ||
                   (math.all(tm1.c2 < -tm1.c3) && math.all(tm2.c2 < -tm2.c3)) ||
                   (math.all(tm1.c2 > tm1.c3) && math.all(tm2.c2 > tm2.c3));
        }

        public static bool IsVisibleAfterFrustumCulling(float4 clipPos)
        {
            return math.all(clipPos.xyz < clipPos.w) && math.all(clipPos.xyz > -clipPos.w);
        }

    }

    [BurstCompile]
    public struct LoadGrassDataJob : IJobParallelFor
    {
        [ReadOnly]
        public float2 CenterPos;
        [ReadOnly]
        public int RenderExtent2;

        public NativeArray<GrassBlockBinding> SceneGrassData;
        //[WriteOnly]
        //public NativeArray<GrassBlockBinding> SceneGrassCatchDatas;
        public void Execute(int index)
        {
            // 用于裁剪的 GrassBlockBinding 信息
            var binding = SceneGrassData[index];
            var bounds = binding.Bounds;
            var p = math.pow(math.abs(CenterPos - bounds.Center.xz), 2);
            var dis = p.x + p.y;
            // 预裁剪，如果当前草的 bounds 包围盒与玩家自己的距离大于植被渲染范围，则需要预裁剪。
            if (dis > (1<<(2* RenderExtent2)))
            {
                binding.Lod = 255;
            }
            else
            {
                binding.Lod = 0;
            }
            // BurstCompile 里面没有引用类型，所以得重新赋值。
            SceneGrassData[index] = binding;
        }
    }

    [BurstCompile]
    public struct CollectGrassDataJob : IJob
    {
        [ReadOnly]
        public NativeArray<GrassBlockBinding> SceneGrassData;
        [WriteOnly]
        public NativeArray<GrassBlockBinding> GrassCacheData;
        [WriteOnly]
        public NativeArray<int> LengthResult;
        public void Execute()
        {
            int index = 0;
            foreach (var data in SceneGrassData)
            {
                if (data.Lod == 0)
                {
                    // 没有被预裁剪的数据放到这里
                    GrassCacheData[index] = data;
                    index++;
                }
            }
            LengthResult[0] = index;
        }
    }

    [BurstCompile]
    public struct FrustumCullingGrassJob : IJobParallelFor
    {
        [ReadOnly]
        public float4x4 FVP;               //vp矩阵 视锥裁剪 由view 到 projection的 NDC裁剪
        [ReadOnly]
        public float3 CameraPos;
        [ReadOnly]
        public float3 GrassLod;
        [ReadOnly] 
        public int MaxLodNum;

        // 草的块信息
        public NativeArray<GrassBlockBinding> BindingDatas;
     
        public void Execute(int index)
        {
            // 获取 index 的草块
            var binding = BindingDatas[index];
            //if (binding.Length == 0)
            //    return;
            int lodNum = MaxLodNum;
            
            // 草块的范围
            var bounds = binding.Bounds;
            float2 p0 = CameraPos.xz;
            float2 p1 = new float2(bounds.Center.x, bounds.Center.z);
            // 草块到摄像机中心点的距离
            var p2 = math.pow(math.abs(p1 - p0), 2);
            var dis = p2.x + p2.y;
            binding.Dist = dis;
            if (dis > GrassLod[lodNum - 1] * GrassLod[lodNum - 1]) //大于最大距离裁剪
            {
                binding.Lod = 255;
                BindingDatas[index] = binding;
                return;
            }

            // 执行裁剪的核心算法
            byte isVisible = FrustumCulling.FrustumCullingBounds(bounds, FVP);

            if (isVisible == 1)
            {
                for (byte j = 0; j < lodNum; j++)
                {
                    if (dis < GrassLod[j] * GrassLod[j])
                    {
                        binding.Lod = j;
                        break;
                    }
                }
            }
            else
            {
                binding.Lod = 255;
            }

            BindingDatas[index] = binding;
        }
    }

    [BurstCompile]
    public struct SortGrassDataJob : IJob
    {
        [ReadOnly]
        public int LodNum;
        [ReadOnly]
        public NativeArray<GrassBlockBinding> GrassDatas;

        [WriteOnly]
        public NativeArray<int> Num;

        public NativeArray<GrassBlockBinding> GrassResult;

        // 0 - index count per instance
        // 1 - instance count
        // 2 - start index location
        // 3 - base vertex location
        // 4 - start instance location
        public NativeArray<uint> Args;

        public void Execute()
        {
            int grassNum = 0;
            int bindingNum = 0;
            // 初始化 Args, Args 每隔5个值，就会填一轮数据
            for (int i = 0; i < Args.Length; i+=5)
            {
                Args[i + 1] = 0;
            }
            // GrassBlockBinding 是用来裁剪和 Lod 排序的数据
            foreach (var data in GrassDatas)
            {
                if (data.Length == 0 || data.Lod == 255)
                    continue;
                // 构建初步渲染数据 GrassResult
                GrassResult[bindingNum] = data;
                Args[(data.Type * LodNum + data.Lod) * 5 + 1] += (uint)data.Length;
                grassNum += data.Length;
                bindingNum++;
            }

            if (bindingNum > 0)
            {
                // 把初步渲染数据 GrassResult 放到 temp01进行排序
                var temp01 = new NativeArray<GrassBlockBinding>(bindingNum, Allocator.Temp);
                NativeArray<GrassBlockBinding>.Copy(GrassResult, 0, temp01, 0, bindingNum);
                // 开始排序
                Sort(temp01, 0, bindingNum - 1);
                // 把排序完的 temp01 数据放回 GrassResult
                NativeArray<GrassBlockBinding>.Copy(temp01, 0, GrassResult, 0, bindingNum);
                temp01.Dispose();
            }
            Num[0] = bindingNum;
            Num[1] = grassNum;
            // 本 Lod 的 base vertex location 等于 上一个 Lod 的 instance count 数量 + start instance location 起始位置
            for (int i = 5; i < Args.Length; i += 5)
            {
                Args[i + 4] = Args[i - 4] + Args[i - 1];
            }
        }

        // 快速排序
        private int SortUnit(NativeArray<GrassBlockBinding> array, int low, int high)
        {
            GrassBlockBinding key = array[low];
            while (low < high)
            {
                /*从后向前搜索比key小的值*/
                while (array[high].Sort(key) == 1 && high > low)
                    --high;
                /*比key小的放左边*/
                array[low] = array[high];
                /*从前向后搜索比key大的值，比key大的放右边*/
                while (array[low].Sort(key) == -1 && high > low)
                    ++low;
                /*比key大的放右边*/
                array[high] = array[low];
            }
            /*左边都比key小，右边都比key大。//将key放在游标当前位置。//此时low等于high */
            array[low] = key;

            return high;
        }

        public void Sort(NativeArray<GrassBlockBinding> array, int low, int high)
        {
            if (low >= high)
                return;
            /*完成一次单元排序*/
            int index = SortUnit(array, low, high);
            /*对左边单元进行排序*/
            Sort(array, low, index - 1);
            /*对右边单元进行排序*/
            Sort(array, index + 1, high);
        }
    }

}
