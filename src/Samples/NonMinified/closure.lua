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
        "26S26O27526R27527821C24U24W26P26T27823K23M24924E23O26P27327823O24523N23O26624C23P24125S24C24F24124424524426P26Z27823Q24127H24124224C24525S25P25S27027826A27727526P27826U28L28L28K26O28P27828T26O26A26826O26Q28Y28H27626O28J28T28Z28Z28L26A27529629129326O27428U29F28U29D27729H29029729I28S28R27828S27D27F27H27J29H26A26V29R28T27M29P29A27829N26O28428Q29I26O"
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

    W = BitXOR(W, 240)
    X = BitXOR(X, 240)
    Y = BitXOR(Y, 240)
    Z = BitXOR(Z, 240)

    Pos = Pos + 4
    return (Z * 16777216) + (Y * 65536) + (X * 256) + W
end

local function gBits8()
    local F = BitXOR(Byte(ByteString, Pos, Pos), 240)
    Pos = Pos + 1
    return F
end

local function gBits16()
    local W, X = Byte(ByteString, Pos, Pos + 2)

    W = BitXOR(W, 240)
    X = BitXOR(X, 240)

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
        FStr[Idx] = Char(BitXOR(Byte(Sub(Str, Idx, Idx)), 240))
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
        elseif (Type == 3) then
            Cons = gFloat()
        elseif (Type == 1) then
            Cons = gString()
        end

        Consts[Idx] = Cons
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
            if Enum <= 7 then
                if Enum <= 3 then
                    if Enum <= 1 then
                        if Enum == 0 then
                            Stk[Inst[2]] = Stk[Inst[3]]
                        else
                            local NewProto = Proto[Inst[3]]
                            local NewUvals
                            local Indexes = {}
                            NewUvals =
                            Setmetatable(
                                    {},
                                    {__index = function(_, Key)
                                        local Val = Indexes[Key]
                                        return Val[1][Val[2]]
                                    end, __newindex = function(_, Key, Value)
                                        local Val = Indexes[Key]
                                        Val[1][Val[2]] = Value
                                    end}
                            )
                            for Idx = 1, Inst[4] do
                                InstrPoint = InstrPoint + 1
                                local Mvm = Instr[InstrPoint]
                                if Mvm[1] == 0 then
                                    Indexes[Idx - 1] = {Stk, Mvm[3]}
                                else
                                    Indexes[Idx - 1] = {Upvalues, Mvm[3]}
                                end
                                Lupvals[#Lupvals + 1] = Indexes
                            end
                            Stk[Inst[2]] = Wrap(NewProto, NewUvals, Env)
                        end
                    elseif Enum == 2 then
                        local A = Inst[2]
                        Stk[A](Stk[A + 1])
                    else
                        Stk[Inst[2]] = Inst[3]
                    end
                elseif Enum <= 5 then
                    if Enum > 4 then
                        Stk[Inst[2]] = Env[Inst[3]]
                    else
                        local A = Inst[2]
                        Stk[A](Stk[A + 1])
                    end
                elseif Enum == 6 then
                    do
                        return
                    end
                else
                    Stk[Inst[2]] = Env[Inst[3]]
                end
            elseif Enum <= 11 then
                if Enum <= 9 then
                    if Enum == 8 then
                        Stk[Inst[2]] = Stk[Inst[3]]
                    else
                        local B = Inst[3]
                        local K = Stk[B]
                        for Idx = B + 1, Inst[4] do
                            K = K .. Stk[Idx]
                        end
                        Stk[Inst[2]] = K
                    end
                elseif Enum == 10 then
                    local B = Inst[3]
                    local K = Stk[B]
                    for Idx = B + 1, Inst[4] do
                        K = K .. Stk[Idx]
                    end
                    Stk[Inst[2]] = K
                else
                    do
                        return
                    end
                end
            elseif Enum <= 13 then
                if Enum > 12 then
                    local NewProto = Proto[Inst[3]]
                    local NewUvals
                    local Indexes = {}
                    NewUvals =
                    Setmetatable(
                            {},
                            {__index = function(_, Key)
                                local Val = Indexes[Key]
                                return Val[1][Val[2]]
                            end, __newindex = function(_, Key, Value)
                                local Val = Indexes[Key]
                                Val[1][Val[2]] = Value
                            end}
                    )
                    for Idx = 1, Inst[4] do
                        InstrPoint = InstrPoint + 1
                        local Mvm = Instr[InstrPoint]
                        if Mvm[1] == 0 then
                            Indexes[Idx - 1] = {Stk, Mvm[3]}
                        else
                            Indexes[Idx - 1] = {Upvalues, Mvm[3]}
                        end
                        Lupvals[#Lupvals + 1] = Indexes
                    end
                    Stk[Inst[2]] = Wrap(NewProto, NewUvals, Env)
                else
                    Stk[Inst[2]] = Upvalues[Inst[3]]
                end
            elseif Enum <= 14 then
                Stk[Inst[2]] = Inst[3]
            elseif Enum == 15 then
                Stk[Inst[2]] = Upvalues[Inst[3]]
            else
                local K
                local B
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
                Stk[Inst[2]] = Env[Inst[3]]
                InstrPoint = InstrPoint + 1
                Inst = Instr[InstrPoint]
                Stk[Inst[2]] = Inst[3]
                InstrPoint = InstrPoint + 1
                Inst = Instr[InstrPoint]
                Stk[Inst[2]] = Stk[Inst[3]]
                InstrPoint = InstrPoint + 1
                Inst = Instr[InstrPoint]
                B = Inst[3]
                K = Stk[B]
                for Idx = B + 1, Inst[4] do
                    K = K .. Stk[Idx]
                end
                Stk[Inst[2]] = K
                InstrPoint = InstrPoint + 1
                Inst = Instr[InstrPoint]
                A = Inst[2]
                Stk[A](Stk[A + 1])
                InstrPoint = InstrPoint + 1
                Inst = Instr[InstrPoint]
                do
                    return
                end
            end
            InstrPoint = InstrPoint + 1
        end
    end
end
return Wrap(Deserialize(), {}, GetFEnv())()
