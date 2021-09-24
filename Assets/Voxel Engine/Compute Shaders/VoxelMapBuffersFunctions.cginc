#ifndef VFUNC
#define VFUNC

int to1D(int3 pos, int3 size) {
    return pos.x + pos.y * size.x + pos.z * size.x * size.y;
}

bool IsOutOfBounds(int3 pos)
{
    return (pos.x <= -m_size_x || pos.x >= m_size_x  * 2 ||
            pos.y <= -m_size_y || pos.y >= m_size_y  * 2 ||
            pos.z <= -m_size_z || pos.z >= m_size_z  * 2);
}

int GetMaskValue(int pos, int size)
{
    return pos < 0 ? -1 : (pos >= size ? 1 : 0);
}

uint GetBlock(int3 pos)
{
    if (IsOutOfBounds(pos)) 
        return 1;

    int3 SIZE = int3(m_size_x, m_size_y, m_size_z);

    int3 mask = int3(
        GetMaskValue(pos.x, m_size_x),
        GetMaskValue(pos.y, m_size_y),
        GetMaskValue(pos.z, m_size_z));

    pos = pos - mask * SIZE;
    
    if (mask.x < 0)
    {
        if (mask.y < 0)
        {
            if (mask.z < 0)
            {
                return Map___[to1D(pos, SIZE)];
            }
            else if (mask.z == 0)
            {
                return Map__0[to1D(pos, SIZE)];
            }
            else // mask.z > 0
            {
                return Map__1[to1D(pos, SIZE)];
            }
        }
        else if (mask.y == 0)
        {
            if (mask.z < 0)
            {
                return Map_0_[to1D(pos, SIZE)];
            }
            else if (mask.z == 0)
            {
                return Map_00[to1D(pos, SIZE)];
            }
            else // mask.z > 0
            {
                return Map_01[to1D(pos, SIZE)];
            }
        }
        else // mask.y > 0
        {
            if (mask.z < 0)
            {
                return Map_1_[to1D(pos, SIZE)];
            }
            else if (mask.z == 0)
            {
                return Map_10[to1D(pos, SIZE)];
            }
            else // mask.z > 0
            {
                return Map_11[to1D(pos, SIZE)];
            }
        }
    }
    else if (mask.x == 0)
    {
        if (mask.y < 0)
        {
            if (mask.z < 0)
            {
                return Map0__[to1D(pos, SIZE)];
            }
            else if (mask.z == 0)
            {
                return Map0_0[to1D(pos, SIZE)];
            }
            else // mask.z > 0
            {
                return Map0_1[to1D(pos, SIZE)];
            }
        }
        else if (mask.y == 0)
        {
            if (mask.z < 0)
            {
                return Map00_[to1D(pos, SIZE)];
            } 
            else if (mask.z == 0)
            {
                return Map[to1D(pos, SIZE)];
            }
            else // mask.z > 0
            {
                return Map001[to1D(pos, SIZE)];
            }
        }
        else // mask.y > 0
        {
            if (mask.z < 0)
            {
                return Map01_[to1D(pos, SIZE)];
            }
            else if (mask.z == 0)
            {
                return Map010[to1D(pos, SIZE)];
            }
            else // mask.z > 0
            {
                return Map011[to1D(pos, SIZE)];
            }
        }
    }
    else // mask.x > 0
    {
        if (mask.y < 0)
        {
            if (mask.z < 0)
            {
                return Map1__[to1D(pos, SIZE)];
            }
            else if (mask.z == 0)
            {
                return Map1_0[to1D(pos, SIZE)];
            }
            else // mask.z > 0
            {
                return Map1_1[to1D(pos, SIZE)];
            }
        }
        else if (mask.y == 0)
        {
            if (mask.z < 0)
            {
                return Map10_[to1D(pos, SIZE)];
            }
            else if (mask.z == 0)
            {
                return Map100[to1D(pos, SIZE)];
            }
            else // mask.z > 0
            {
                return Map101[to1D(pos, SIZE)];
            }
        }
        else // mask.y > 0
        {
            if (mask.z < 0)
            {
                return Map11_[to1D(pos, SIZE)];
            }
            else if (mask.z == 0)
            {
                return Map110[to1D(pos, SIZE)];
            }
            else // mask.z > 0
            {
                return Map111[to1D(pos, SIZE)];
            }
        }
    }
}

#endif