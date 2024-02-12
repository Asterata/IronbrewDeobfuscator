local d=string.byte;local f=string.char;local c=string.sub;local b=table.concat;local s=table.insert;local C=math.ldexp;local S=getfenv or function()return _ENV end;local l=setmetatable;local h=select;local r=unpack or table.unpack;local u=tonumber;local function H(d)local e,n,a="","",{}local o=256;local t={}for l=0,o-1 do t[l]=f(l)end;local l=1;local function i()local e=u(c(d,l,l),36)l=l+1;local n=u(c(d,l,l+e-1),36)l=l+e;return n end;e=f(i())a[1]=e;while l<#d do local l=i()if t[l]then n=t[l]else n=e..c(e,1,1)end;t[o]=e..c(n,1,1)a[#a+1],e,o=n,n,o+1 end;return table.concat(a)end;local i=H('24424C27524F24927524C26K26M26T26Q26G24F24D27927127927K23S26427G27927324F24827926Q26X26C27F27H27526Z27O27526S28124C26T24C23S27923Y24027927Y24C23Y27527H24E28927528I24F28J24C28M27S27K27928M27H28F27927828R27H28M27528V28I24A28N28M24B28N27S27428W24C27H27S27Y24624C28I28I27524823W27923X29C28E24724C27828D24C24324C29429027529W29728Q28G29S24C29727H24A2412792AA28H24C29L2AB29P28728C28R275');local o=bit and bit.bxor or function(l,e)local n,o=1,0
    while l>0 and e>0 do
        local a,c=l%2,e%2
        if a~=c then o=o+n end
        l,e,n=(l-a)/2,(e-c)/2,n*2
    end
    if l<e then l=e end
    while l>0 do
        local e=l%2
        if e>0 then o=o+n end
        l,n=(l-e)/2,n*2
    end
    return o
end
local function n(e,l,n)if n then
    local l=(e/2^(l-1))%2^((n-1)-(l-1)+1);return l-l%1;else
    local l=2^(l-1);return(e%(l+l)>=l)and 1 or 0;end;end;local l=1;local function e()local n,c,a,e=d(i,l,l+3);n=o(n,156)c=o(c,156)a=o(a,156)e=o(e,156)l=l+4;return(e*16777216)+(a*65536)+(c*256)+n;end;local function t()local e=o(d(i,l,l),156);l=l+1;return e;end;local function a()local e,n=d(i,l,l+2);e=o(e,156)n=o(n,156)l=l+2;return(n*256)+e;end;local function H()local l=e();local e=e();local c=1;local o=(n(e,1,20)*(2^32))+l;local l=n(e,21,31);local e=((-1)^n(e,32));if(l==0)then
    if(o==0)then
        return e*0;else
        l=1;c=0;end;elseif(l==2047)then
    return(o==0)and(e*(1/0))or(e*(0/0));end;return C(e,l-1023)*(c+(o/(2^52)));end;local u=e;local function C(e)local n;if(not e)then
    e=u();if(e==0)then
        return'';end;end;n=c(i,l,l+e-1);l=l+e;local e={}for l=1,#n do
    e[l]=f(o(d(c(n,l,l)),156))end
    return b(e);end;local l=e;local function u(...)return{...},h('#',...)end
local function i()local f={};local r={};local l={};local d={f,r,nil,l};local l=e()local c={}for n=1,l do
    local e=t();local l;if(e==2)then l=(t()~=0);elseif(e==0)then l=H();elseif(e==3)then l=C();end;c[n]=l;end;d[3]=t();for d=1,e()do
    local l=t();if(n(l,1,1)==0)then
        local o=n(l,2,3);local t=n(l,4,6);local l={a(),a(),nil,nil};if(o==0)then
            l[3]=a();l[4]=a();elseif(o==1)then
            l[3]=e();elseif(o==2)then
            l[3]=e()-(2^16)elseif(o==3)then
            l[3]=e()-(2^16)l[4]=a();end;if(n(t,1,1)==1)then l[2]=c[l[2]]end
        if(n(t,2,2)==1)then l[3]=c[l[3]]end
        if(n(t,3,3)==1)then l[4]=c[l[4]]end
        f[d]=l;end
end;for l=1,e()do r[l-1]=i();end;return d;end;local function f(l,e,a)local n=l[1];local e=l[2];local l=l[3];return function(...)local c=n;local e=e;local o=l;local l=u
    local n=1;local l=-1;local i={};local t={...};local d=h('#',...)-1;local l={};local e={};for l=0,d do
        if(l>=o)then
            i[l-o]=t[l+1];else
            e[l]=t[l+1];end;end;local l=d-o+1
    local l;local o;while true do
        l=c[n];o=l[1];if o<=9 then if o<=4 then if o<=1 then if o>0 then
            local n=l[2]e[n](r(e,n+1,l[3]))else do return end;end;elseif o<=2 then e[l[2]]={};elseif o==3 then
            local n=l[2]e[n](r(e,n+1,l[3]))else e[l[2]]=e[l[3]];end;elseif o<=6 then if o==5 then
            local n=l[2];local o=e[n];for l=n+1,l[3]do
                s(o,e[l])end;else for l=l[2],l[3]do e[l]=nil;end;end;elseif o<=7 then e[l[2]]=l[3];elseif o>8 then
            local o=l[2];local a=l[4];local c=o+2
            local o={e[o](e[o+1],e[c])};for l=1,a do
                e[c+l]=o[l];end;local o=o[1]if o then
                e[c]=o
                n=l[3];else
                n=n+1;end;else e[l[2]]={};end;elseif o<=14 then if o<=11 then if o>10 then e[l[2]]=a[l[3]];else for l=l[2],l[3]do e[l]=nil;end;end;elseif o<=12 then local o;e[l[2]]=a[l[3]];n=n+1;l=c[n];e[l[2]]=l[3];n=n+1;l=c[n];e[l[2]]=l[3];n=n+1;l=c[n];e[l[2]]=l[3];n=n+1;l=c[n];o=l[2]e[o](r(e,o+1,l[3]))n=n+1;l=c[n];e[l[2]]=a[l[3]];n=n+1;l=c[n];e[l[2]]={};n=n+1;l=c[n];e[l[2]]=l[3];n=n+1;l=c[n];e[l[2]]=l[3];n=n+1;l=c[n];e[l[2]]=l[3];elseif o==13 then
            local o=l[2];local a=l[4];local c=o+2
            local o={e[o](e[o+1],e[c])};for l=1,a do
                e[c+l]=o[l];end;local o=o[1]if o then
                e[c]=o
                n=l[3];else
                n=n+1;end;else e[l[2]]=l[3];end;elseif o<=17 then if o<=15 then e[l[2]]=e[l[3]];elseif o>16 then
            local n=l[2];local o=e[n];for l=n+1,l[3]do
                s(o,e[l])end;else n=l[3];end;elseif o<=18 then e[l[2]]=a[l[3]];elseif o>19 then do return end;else n=l[3];end;n=n+1;end;end;end;return f(i(),{},S())();