using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NetEZ.Core.IO
{
    public class BinaryIOBuffer
    {
        private byte[] _PartialBuffer = null;
        private int _PartialBufferOffset = 0;
        private int _MessageMinLength = 4;
        private int _MessageMaxLength = Defines.MAX_MESSAGE_LENGTH;

        public BinaryIOBuffer(int minLen, int maxLen)
        {
            _MessageMinLength = minLen;
            _MessageMaxLength = maxLen;
        }

        public void Reset()
        {
            _PartialBuffer = null;
            _PartialBufferOffset = 0;
        }

        private bool IsMessageLengthValid(int len)
        {
            if (_MessageMinLength <= len && len <= _MessageMaxLength)
                return true;

            return false;
        }

        private bool ReadMessage(byte[] buffer, int bytes, ref byte[] partialBytes, ref int partialOffset, ref List<byte[]> rawMsgBytesList)
        {
            if (buffer == null || buffer.Length < 1 || bytes < 1 || bytes > buffer.Length)
                return false;

            int readBufferOffset = 0;

            //  解析数据包的情景
            //  1. 新数据包, 读出N（N >= 1）条消息，剩余M（M >= 0）字节是另一条消息的开头部分
            //  2. 上次剩余的字节(partialBytes)，结合本次buffer，读出N条消息，剩余M字节

            if (partialBytes != null)
            {
                //  partialBytes 定义：接收的字节数不足以构成一条完整的消息块
                //  处理partialBytes的几种结果
                //  1. partialBytes 本身不完整（长度小于4），没有在构建时固定长度，因此需要在收到后续字节后重构partialBytes
                //  2. partialBytes 本次从接收缓冲区填补完成
                //  3. partialBytes 本次从接收缓冲区填补字节，但仍未完成(整个消息字节块)

                if (partialBytes.Length < 4)
                {
                    //  上次剩余的字节不足4个，因此partialBytes的长度没有预先固定，本次需要合并partialBytes与buffer
                    byte[] tmpBuf = new byte[partialBytes.Length + bytes];
                    Buffer.BlockCopy(partialBytes, 0, tmpBuf, 0, partialBytes.Length);
                    Buffer.BlockCopy(buffer, 0, tmpBuf, partialBytes.Length, bytes);

                    partialBytes = null;            //  重置
                    partialOffset = 0;              //  重置
                    //  由于合并了 partialBytes+buffer，将合并后缓冲字节以无partial的方式递归调用
                    if (ReadMessage(tmpBuf, tmpBuf.Length, ref partialBytes, ref partialOffset, ref rawMsgBytesList))
                    {
                        return true;
                    }
                    else
                        return false;
                }
                else
                {
                    //  上次的partialBytes预留了长度，本次从partialOffset处开始填补收到的字节
                    if (partialBytes.Length - partialOffset <= bytes)
                    {
                        //  本次收到的字节填补了上次的partialBytes，而且填完
                        Buffer.BlockCopy(buffer, 0, partialBytes, partialOffset, partialBytes.Length - partialOffset);
                        //  将当前完成的消息字节块加入List
                        if (rawMsgBytesList == null)
                            rawMsgBytesList = new List<byte[]>();

                        rawMsgBytesList.Add(partialBytes);

                        //  read偏移量增加
                        //  设置readBuffer，表示后面还要从剩余的buffer里尝试读出其他消息字节块
                        readBufferOffset += partialBytes.Length - partialOffset;

                        partialBytes = null;            //  重置
                        partialOffset = 0;              //  重置
                    }
                    else
                    {
                        //  本次收到的字节填补了上次的partialBytes，但本次没有填完
                        Buffer.BlockCopy(buffer, 0, partialBytes, partialOffset, bytes);
                        partialOffset += bytes;

                        return true;
                    }
                }
            }

            int msgLen = 0;
            while (readBufferOffset < bytes)
            {
                if (readBufferOffset + 4 <= bytes)
                {
                    //  每个消息块的前面都应有4字节，表示消息块的长度
                    msgLen = BitConverter.ToInt32(buffer, readBufferOffset);

                    readBufferOffset += 4;
                    //  消息块的长度必须合法
                    if (!IsMessageLengthValid(msgLen))
                        return false;

                    byte[] rawMsgBytes = new byte[msgLen];

                    if (msgLen <= bytes - readBufferOffset)
                    {
                        //  如果后面的接收区有足够的字节可以读进来
                        Buffer.BlockCopy(buffer, readBufferOffset, rawMsgBytes, 0, msgLen);
                        readBufferOffset += msgLen;

                        if (rawMsgBytesList == null)
                            rawMsgBytesList = new List<byte[]>();

                        rawMsgBytesList.Add(rawMsgBytes);
                    }
                    else
                    {
                        //  后面的接收区没有足够的字节
                        //  先复制目前接收的字节
                        Buffer.BlockCopy(buffer, readBufferOffset, rawMsgBytes, 0, bytes - readBufferOffset);

                        //  当前未完成的消息字节块暂存到partialBytes,并记录偏移量
                        partialBytes = rawMsgBytes;
                        partialOffset = bytes - readBufferOffset;

                        readBufferOffset = bytes;
                    }
                }
                else
                {
                    //  剩余的接收缓冲区还有不到4个字节
                    partialBytes = new byte[bytes - readBufferOffset];
                    partialOffset = 0;

                    Buffer.BlockCopy(buffer, readBufferOffset, partialBytes, 0, partialBytes.Length);
                    readBufferOffset = bytes;
                }

            }

            return true;
        }

        public bool ReadMessage(byte[] buffer, int bytes, ref List<byte[]> rawMsgBytesList)
        {
            return ReadMessage(buffer, bytes, ref _PartialBuffer, ref _PartialBufferOffset, ref rawMsgBytesList);
        }
    }
}
