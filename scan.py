import sys
p = sys.argv[1]
data = open(p, 'rb').read()
for needle in [b'map.yaml', b'map.bin', b'oramap', b'MapPreview', b'HandshakeResponse', b'f2de6f4f', b'A Nuclear Winter']:
    print(needle, data.find(needle))
