import ctypes as c, os, time
x=c.CDLL('libX11.so.6'); p=c.CDLL('libXpresent.so.1')
def bind(lib,name,result,args):
    fn=getattr(lib,name); fn.restype=result; fn.argtypes=args; return fn
P=c.c_void_p; U=c.c_ulong; I=c.c_int
op=bind(x,'XOpenDisplay',P,[c.c_char_p]); d=op(None); assert d
root=bind(x,'XDefaultRootWindow',U,[P])(d)
screen=bind(x,'XDefaultScreen',I,[P])(d)
depth=bind(x,'XDefaultDepth',I,[P,I])(d,screen)
w=bind(x,'XCreateSimpleWindow',U,[P,U,I,I,c.c_uint,c.c_uint,c.c_uint,U,U])(d,root,60,60,400,200,0,0,0)
atom=bind(x,'XInternAtom',U,[P,c.c_char_p,I])
prop=bind(x,'XChangeProperty',I,[P,U,U,U,I,I,P,I])
title=b'ECP real Present fixture'
prop(d,w,atom(d,b'_NET_WM_NAME',0),atom(d,b'UTF8_STRING',0),8,0,c.cast(c.c_char_p(title),P),len(title))
pid=U(os.getpid()); prop(d,w,atom(d,b'_NET_WM_PID',0),6,32,0,c.byref(pid),1)
bind(x,'XStoreName',I,[P,U,c.c_char_p])(d,w,title)
bind(x,'XMapWindow',I,[P,U])(d,w); flush=bind(x,'XFlush',I,[P]); flush(d); time.sleep(1)
gc=bind(x,'XCreateGC',P,[P,U,U,P])(d,w,0,None)
pix=bind(x,'XCreatePixmap',U,[P,U,c.c_uint,c.c_uint,c.c_uint])
fill=bind(x,'XFillRectangle',I,[P,U,P,I,I,c.c_uint,c.c_uint]); color=bind(x,'XSetForeground',I,[P,P,U])
images=[]
for index in range(2):
    image=pix(d,w,400,200,depth); color(d,gc,0x00ff00 if index else 0xff0000); fill(d,image,gc,0,0,400,200); images.append(image)
major=I(1); minor=I(2); assert bind(p,'XPresentQueryVersion',I,[P,P,P])(d,c.byref(major),c.byref(minor))
present=bind(p,'XPresentPixmap',None,[P,U,U,c.c_uint,U,U,I,I,U,U,U,c.c_uint,c.c_uint64,c.c_uint64,c.c_uint64,P,I])
start=time.monotonic()
for frame in range(240):
    present(d,w,images[frame%2],frame+1,0,0,0,0,0,0,0,0,0,0,0,None,0); flush(d)
    time.sleep(max(0,start+(frame+1)/30-time.monotonic()))
bind(x,'XDestroyWindow',I,[P,U])(d,w); bind(x,'XCloseDisplay',I,[P])(d)