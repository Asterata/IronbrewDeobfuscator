local Byte = string.byte
local Char = string.char
local Sub = string.sub
local Concat = table.concat
local Insert = table.insert
local LDExp = math.ldexp
local GetFEnv = getfenv or function()
    return _ENV
end
local Setmetatable = setmetatable
local Select = select

local Unpack = unpack or table.unpack
local ToNumber = tonumber
local function decompress(b)
    local c, d, e = "", "", {}
    local f = 256
    local g = {}
    for h = 0, f - 1 do
        g[h] = Char(h)
    end
    local i = 1
    local function k()
        local l = ToNumber(Sub(b, i, i), 36)
        i = i + 1
        local m = ToNumber(Sub(b, i, i + l - 1), 36)
        i = i + l
        return m
    end
    c = Char(k())
    e[1] = c
    while i < #b do
        local n = k()
        if g[n] then
            d = g[n]
        else
            d = c .. Sub(c, 1, 1)
        end
        g[f] = c .. Sub(d, 1, 1)
        e[#e + 1], c, f = d, d, f + 1
    end
    return table.concat(e)
end
local ByteString =
decompress(
        "24424C27524F24927524C26K26M26T26Q26G24F24D27927127927K23S26427G27927324F24827926Q26X26C27F27H27526Z27O27526S28124C26T24C23S27923Y24027927Y24C23Y27527H24E28927528I24F28J24C28M27S27K27928M27H28F27927828R27H28M27528V28I24A28N28M24B28N27S27428W24C27H27S27Y24624C28I28I27524823W27923X29C28E24724C27828D24C24324C29429027529W29728Q28G29S24C29727H24A2412792AA28H24C29L2AB29P28728C28R275"
)

local BitXOR = bit and bit.bxor or function(a, b)
    local p, c = 1, 0
    while a > 0 and b > 0 do
        local ra, rb = a % 2, b % 2
        if ra ~= rb then
            c = c + p
        end
        a, b, p = (a - ra) / 2, (b - rb) / 2, p * 2
    end
    if a < b then
        a = b
    end
    while a > 0 do
        local ra = a % 2
        if ra > 0 then
            c = c + p
        end
        a, p = (a - ra) / 2, p * 2
    end
    return c
end

local function gBit(Bit, Start, End)
    if End then
        local Res = (Bit / 2 ^ (Start - 1)) % 2 ^ ((End - 1) - (Start - 1) + 1)
        return Res - Res % 1
    else
        local Plc = 2 ^ (Start - 1)
        return (Bit % (Plc + Plc) >= Plc) and 1 or 0
    end
end

local Pos = 1

local function gBits32()
    local W, X, Y, Z = Byte(ByteString, Pos, Pos + 3)

    W = BitXOR(W, 156)
    X = BitXOR(X, 156)
    Y = BitXOR(Y, 156)
    Z = BitXOR(Z, 156)

    Pos = Pos + 4
    return (Z * 16777216) + (Y * 65536) + (X * 256) + W
end

local function gBits8()
    local F = BitXOR(Byte(ByteString, Pos, Pos), 156)
    Pos = Pos + 1
    return F
end

local function gBits16()
    local W, X = Byte(ByteString, Pos, Pos + 2)

    W = BitXOR(W, 156)
    X = BitXOR(X, 156)

    Pos = Pos + 2
    return (X * 256) + W
end

local function gFloat()
    local Left = gBits32()
    local Right = gBits32()
    local IsNormal = 1
    local Mantissa = (gBit(Right, 1, 20) * (2 ^ 32)) + Left
    local Exponent = gBit(Right, 21, 31)
    local Sign = ((-1) ^ gBit(Right, 32))
    if (Exponent == 0) then
        if (Mantissa == 0) then
            return Sign * 0 -- +-0
        else
            Exponent = 1
            IsNormal = 0
        end
    elseif (Exponent == 2047) then
        return (Mantissa == 0) and (Sign * (1 / 0)) or (Sign * (0 / 0))
    end
    return LDExp(Sign, Exponent - 1023) * (IsNormal + (Mantissa / (2 ^ 52)))
end

local gSizet = gBits32
local function gString(Len)
    local Str
    if (not Len) then
        Len = gSizet()
        if (Len == 0) then
            return ""
        end
    end

    Str = Sub(ByteString, Pos, Pos + Len - 1)
    Pos = Pos + Len

    local FStr = {}
    for Idx = 1, #Str do
        FStr[Idx] = Char(BitXOR(Byte(Sub(Str, Idx, Idx)), 156))
    end

    return Concat(FStr)
end

local gInt = gBits32
local function _R(...)
    return {...}, Select("#", ...)
end

local function Deserialize()
    local Instrs = {}
    local Functions = {}
    local Lines = {}
    local Chunk = {
        Instrs,
        Functions,
        nil,
        Lines
    }
    local ConstCount = gBits32()
    local Consts = {}

    for Idx = 1, ConstCount do
        local Type = gBits8()
        local Cons

        if (Type == 2) then
            Cons = (gBits8() ~= 0)
        elseif (Type == 0) then
            Cons = gFloat()
        elseif (Type == 3) then
            Cons = gString()
        end

        Consts[Idx] = Cons
    end
    Chunk[3] = gBits8()
    for Idx = 1, gBits32() do
        local Descriptor = gBits8()
        if (gBit(Descriptor, 1, 1) == 0) then
            local Type = gBit(Descriptor, 2, 3)
            local Mask = gBit(Descriptor, 4, 6)

            local Inst = {
                gBits16(),
                gBits16(),
                nil,
                nil
            }

            if (Type == 0) then
                Inst[3] = gBits16()
                Inst[4] = gBits16()
            elseif (Type == 1) then
                Inst[3] = gBits32()
            elseif (Type == 2) then
                Inst[3] = gBits32() - (2 ^ 16)
            elseif (Type == 3) then
                Inst[3] = gBits32() - (2 ^ 16)
                Inst[4] = gBits16()
            end

            if (gBit(Mask, 1, 1) == 1) then
                Inst[2] = Consts[Inst[2]]
            end
            if (gBit(Mask, 2, 2) == 1) then
                Inst[3] = Consts[Inst[3]]
            end
            if (gBit(Mask, 3, 3) == 1) then
                Inst[4] = Consts[Inst[4]]
            end

            Instrs[Idx] = Inst
        end
    end
    for Idx = 1, gBits32() do
        Functions[Idx - 1] = Deserialize()
    end
    return Chunk
end
local function Wrap(Chunk, Upvalues, Env)
    local Instr = Chunk[1]
    local Proto = Chunk[2]
    local Params = Chunk[3]

    return function(...)
        local Instr = Instr
        local Proto = Proto
        local Params = Params

        local _R = _R
        local InstrPoint = 1
        local Top = -1

        local Vararg = {}
        local Args = {...}

        local PCount = Select("#", ...) - 1

        local Lupvals = {}
        local Stk = {}

        for Idx = 0, PCount do
            if (Idx >= Params) then
                Vararg[Idx - Params] = Args[Idx + 1]
            else
                Stk[Idx] = Args[Idx + 1]
            end
        end

        local Varargsz = PCount - Params + 1

        local Inst
        local Enum

        while true do
            Inst = Instr[InstrPoint]
            Enum = Inst[1]
            if Enum <= 9 then
                if Enum <= 4 then
                    if Enum <= 1 then
                        if Enum > 0 then
                            local A = Inst[2]
                            Stk[A](Unpack(Stk, A + 1, Inst[3]))
                        else
                            do
                                return
                            end
                        end
                    elseif Enum <= 2 then
                        Stk[Inst[2]] = {}
                    elseif Enum == 3 then
                        local A = Inst[2]
                        Stk[A](Unpack(Stk, A + 1, Inst[3]))
                    else
                        Stk[Inst[2]] = Stk[Inst[3]]
                    end
                elseif Enum <= 6 then
                    if Enum == 5 then
                        local A = Inst[2]
                        local T = Stk[A]
                        for Idx = A + 1, Inst[3] do
                            Insert(T, Stk[Idx])
                        end
                    else
                        for Idx = Inst[2], Inst[3] do
                            Stk[Idx] = nil
                        end
                    end
                elseif Enum <= 7 then
                    Stk[Inst[2]] = Inst[3]
                elseif Enum > 8 then
                    local A = Inst[2]
                    local C = Inst[4]
                    local CB = A + 2
                    local Result = {Stk[A](Stk[A + 1], Stk[CB])}
                    for Idx = 1, C do
                        Stk[CB + Idx] = Result[Idx]
                    end
                    local R = Result[1]
                    if R then
                        Stk[CB] = R
                        InstrPoint = Inst[3]
                    else
                        InstrPoint = InstrPoint + 1
                    end
                else
                    Stk[Inst[2]] = {}
                end
            elseif Enum <= 14 then
                if Enum <= 11 then
                    if Enum > 10 then
                        Stk[Inst[2]] = Env[Inst[3]]
                    else
                        for Idx = Inst[2], Inst[3] do
                            Stk[Idx] = nil
                        end
                    end
                elseif Enum <= 12 then
                    local A
                    Stk[Inst[2]] = Env[Inst[3]]
                    InstrPoint = InstrPoint + 1
                    Inst = Instr[InstrPoint]
                    Stk[Inst[2]] = Inst[3]
                    InstrPoint = InstrPoint + 1
                    Inst = Instr[InstrPoint]
                    Stk[Inst[2]] = Inst[3]
                    InstrPoint = InstrPoint + 1
                    Inst = Instr[InstrPoint]
                    Stk[Inst[2]] = Inst[3]
                    InstrPoint = InstrPoint + 1
                    Inst = Instr[InstrPoint]
                    A = Inst[2]
                    Stk[A](Unpack(Stk, A + 1, Inst[3]))
                    InstrPoint = InstrPoint + 1
                    Inst = Instr[InstrPoint]
                    Stk[Inst[2]] = Env[Inst[3]]
                    InstrPoint = InstrPoint + 1
                    Inst = Instr[InstrPoint]
                    Stk[Inst[2]] = {}
                    InstrPoint = InstrPoint + 1
                    Inst = Instr[InstrPoint]
                    Stk[Inst[2]] = Inst[3]
                    InstrPoint = InstrPoint + 1
                    Inst = Instr[InstrPoint]
                    Stk[Inst[2]] = Inst[3]
                    InstrPoint = InstrPoint + 1
                    Inst = Instr[InstrPoint]
                    Stk[Inst[2]] = Inst[3]
                elseif Enum == 13 then
                    local A = Inst[2]
                    local C = Inst[4]
                    local CB = A + 2
                    local Result = {Stk[A](Stk[A + 1], Stk[CB])}
                    for Idx = 1, C do
                        Stk[CB + Idx] = Result[Idx]
                    end
                    local R = Result[1]
                    if R then
                        Stk[CB] = R
                        InstrPoint = Inst[3]
                    else
                        InstrPoint = InstrPoint + 1
                    end
                else
                    Stk[Inst[2]] = Inst[3]
                end
            elseif Enum <= 17 then
                if Enum <= 15 then
                    Stk[Inst[2]] = Stk[Inst[3]]
                elseif Enum > 16 then
                    local A = Inst[2]
                    local T = Stk[A]
                    for Idx = A + 1, Inst[3] do
                        Insert(T, Stk[Idx])
                    end
                else
                    InstrPoint = Inst[3]
                end
            elseif Enum <= 18 then
                Stk[Inst[2]] = Env[Inst[3]]
            elseif Enum > 19 then
                do
                    return
                end
            else
                InstrPoint = Inst[3]
            end
            InstrPoint = InstrPoint + 1
        end
    end
end
return Wrap(Deserialize(), {}, GetFEnv())()
