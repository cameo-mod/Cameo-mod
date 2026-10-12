import sys, zipfile
p = sys.argv[1]
data = open(p, 'rb').read(64)
print('first64:', data[:32])
print('is_zip:', zipfile.is_zipfile(p))
if zipfile.is_zipfile(p):
    z = zipfile.ZipFile(p)
    for n in z.namelist()[:30]:
        print(' ', n)
