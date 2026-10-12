import sys
t = open(sys.argv[1], encoding='utf-8', errors='replace').read()
print(t[-2000:])
