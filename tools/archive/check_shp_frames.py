import struct, os

def shp_info(path):
    with open(path, 'rb') as f:
        data = f.read()
    count = struct.unpack_from('<H', data, 0)[0]
    count32 = struct.unpack_from('<I', data, 0)[0]
    print(f"  File size: {len(data)} bytes")
    print(f"  uint16 count: {count}")
    print(f"  uint32 count: {count32}")
    print(f"  First 16 bytes: {data[:16].hex()}")
    if count > 0 and count * 8 + 2 <= len(data):
        print(f"  TS format: {count} frames")
        for i in range(min(count, 5)):
            off = 2 + i * 8
            x, y, w, h = struct.unpack_from('<HHHH', data, off)
            print(f"    Frame {i}: x={x}, y={y}, w={w}, h={h}")
    elif count32 > 0 and count32 * 24 + 4 <= len(data):
        print(f"  RA2 format: {count32} frames")

bits = os.path.join(os.path.dirname(__file__), 'mods', 'cameo', 'bits')
for name in ['cabal_rocketcyborg', 'cabal_hackercyborg']:
    fn = name + '.shp'
    for r, _, fs in os.walk(bits):
        if fn in fs:
            p = os.path.join(r, fn)
            print(f"\n{name}:")
            shp_info(p)
            break
