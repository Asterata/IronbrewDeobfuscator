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
        "24J24Q27524Q24V27626226025F25G26624Q24H27625E25R25I25I25H23U26525H26025I25Q27727627126D26W26E26M24P27627622I24528028127526I28528124I26I24Q25A27H27J27L25H28I28J28K28L24Q24S27627O25E28J24Q24I27625G25H28R28K24R27624O28127827527A27C27E24U27626625R26126629027529227629424Q29627D24Q27427527C25G25Q25R25O26725G25L28129927525425427629F24Q25424G24Q29029H27524M2912A827525B2A328129O2A52742902A42AD2862752A92AE2A22AE2A42AT2922AA2AH2922A924W2AE2862582AU2B32AG2AQ2A325127625429Z2B32A52B92AY2812B02BG2762892AT2752892862BN2B92BE2AW27T2A02BP28N2AP2BY29F2572BF29G2B22BI27T2802AA2AT29924T2AP2C92C42802BS24Q2992BU27527829J2AT28O28O2BR2BZ29024S28E2902592AE28024U2A42CY2902D127624X2AS2A22802AV2A729928U2762AC2CF2D42A42502D824Q2DA2BC2DC29N2812DG24Q2BK24Q2B82C4"
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

    W = BitXOR(W, 170)
    X = BitXOR(X, 170)
    Y = BitXOR(Y, 170)
    Z = BitXOR(Z, 170)

    Pos = Pos + 4
    return (Z * 16777216) + (Y * 65536) + (X * 256) + W
end

local function gBits8()
    local F = BitXOR(Byte(ByteString, Pos, Pos), 170)
    Pos = Pos + 1
    return F
end

local function gBits16()
    local W, X = Byte(ByteString, Pos, Pos + 2)

    W = BitXOR(W, 170)
    X = BitXOR(X, 170)

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
        FStr[Idx] = Char(BitXOR(Byte(Sub(Str, Idx, Idx)), 170))
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

        if (Type == 1) then
            Cons = (gBits8() ~= 0)
        elseif (Type == 3) then
            Cons = gFloat()
        elseif (Type == 0) then
            Cons = gString()
        end

        Consts[Idx] = Cons
    end
    for Idx = 1, gBits32() do
        Functions[Idx - 1] = Deserialize()
    end
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
    Chunk[3] = gBits8()
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
            if Enum <= 12 then
                if Enum <= 5 then
                    if Enum <= 2 then
                        if Enum <= 0 then
                            Stk[Inst[2]] = Inst[3]
                        elseif Enum > 1 then
                            InstrPoint = Inst[3]
                        else
                            InstrPoint = Inst[3]
                        end
                    elseif Enum <= 3 then
                        Stk[Inst[2]] = Stk[Inst[3]]
                    elseif Enum == 4 then
                        local T
                        local A
                        Stk[Inst[2]] = Env[Inst[3]]
                        InstrPoint = InstrPoint + 1
                        Inst = Instr[InstrPoint]
                        Stk[Inst[2]] = Inst[3]
                        InstrPoint = InstrPoint + 1
                        Inst = Instr[InstrPoint]
                        A = Inst[2]
                        Stk[A](Stk[A + 1])
                        InstrPoint = InstrPoint + 1
                        Inst = Instr[InstrPoint]
                        Stk[Inst[2]] = Inst[3]
                        InstrPoint = InstrPoint + 1
                        Inst = Instr[InstrPoint]
                        Env[Inst[3]] = Stk[Inst[2]]
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
                        InstrPoint = InstrPoint + 1
                        Inst = Instr[InstrPoint]
                        A = Inst[2]
                        T = Stk[A]
                        for Idx = A + 1, Inst[3] do
                            Insert(T, Stk[Idx])
                        end
                    else
                        local T
                        local A
                        Stk[Inst[2]] = Stk[Inst[3]]
                        InstrPoint = InstrPoint + 1
                        Inst = Instr[InstrPoint]
                        Stk[Inst[2]] = Inst[3]
                        InstrPoint = InstrPoint + 1
                        Inst = Instr[InstrPoint]
                        A = Inst[2]
                        Stk[A](Stk[A + 1])
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
                        InstrPoint = InstrPoint + 1
                        Inst = Instr[InstrPoint]
                        A = Inst[2]
                        T = Stk[A]
                        for Idx = A + 1, Inst[3] do
                            Insert(T, Stk[Idx])
                        end
                    end
                elseif Enum <= 8 then
                    if Enum <= 6 then
                        do
                            return
                        end
                    elseif Enum > 7 then
                        Stk[Inst[2]] = Env[Inst[3]]
                    else
                        Env[Inst[3]] = Stk[Inst[2]]
                    end
                elseif Enum <= 10 then
                    if Enum == 9 then
                        local A
                        Stk[Inst[2]] = Env[Inst[3]]
                        InstrPoint = InstrPoint + 1
                        Inst = Instr[InstrPoint]
                        Stk[Inst[2]] = Stk[Inst[3]]
                        InstrPoint = InstrPoint + 1
                        Inst = Instr[InstrPoint]
                        A = Inst[2]
                        Stk[A](Stk[A + 1])
                        InstrPoint = InstrPoint + 1
                        Inst = Instr[InstrPoint]
                        Stk[Inst[2]] = Env[Inst[3]]
                        InstrPoint = InstrPoint + 1
                        Inst = Instr[InstrPoint]
                        Stk[Inst[2]] = Inst[3]
                        InstrPoint = InstrPoint + 1
                        Inst = Instr[InstrPoint]
                        A = Inst[2]
                        Stk[A](Stk[A + 1])
                    else
                        Stk[Inst[2]] = Inst[3]
                    end
                elseif Enum > 11 then
                    local A = Inst[2]
                    Stk[A](Stk[A + 1])
                else
                    Stk[Inst[2]] = {}
                end
            elseif Enum <= 19 then
                if Enum <= 15 then
                    if Enum <= 13 then
                        local A = Inst[2]
                        Stk[A](Stk[A + 1])
                    elseif Enum > 14 then
                        if (Stk[Inst[2]] ~= Stk[Inst[4]]) then
                            InstrPoint = InstrPoint + 1
                        else
                            InstrPoint = Inst[3]
                        end
                    else
                        Env[Inst[3]] = Stk[Inst[2]]
                    end
                elseif Enum <= 17 then
                    if Enum > 16 then
                        Stk[Inst[2]] = Stk[Inst[3]]
                    else
                        local A = Inst[2]
                        local T = Stk[A]
                        for Idx = A + 1, Inst[3] do
                            Insert(T, Stk[Idx])
                        end
                    end
                elseif Enum > 18 then
                    Stk[Inst[2]] = Wrap(Proto[Inst[3]], nil, Env)
                else
                    Stk[Inst[2]] = Env[Inst[3]]
                end
            elseif Enum <= 22 then
                if Enum <= 20 then
                    if (Stk[Inst[2]] ~= Stk[Inst[4]]) then
                        InstrPoint = InstrPoint + 1
                    else
                        InstrPoint = Inst[3]
                    end
                elseif Enum > 21 then
                    Stk[Inst[2]]()
                else
                    do
                        return
                    end
                end
            elseif Enum <= 24 then
                if Enum == 23 then
                    Stk[Inst[2]]()
                else
                    Stk[Inst[2]] = {}
                end
            elseif Enum == 25 then
                local A = Inst[2]
                local T = Stk[A]
                for Idx = A + 1, Inst[3] do
                    Insert(T, Stk[Idx])
                end
            else
                Stk[Inst[2]] = Wrap(Proto[Inst[3]], nil, Env)
            end
            InstrPoint = InstrPoint + 1
        end
    end
end
return Wrap(Deserialize(), {}, GetFEnv())()
