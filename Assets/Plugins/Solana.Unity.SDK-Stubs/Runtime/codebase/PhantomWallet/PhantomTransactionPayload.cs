using System;

// ReSharper disable once CheckNamespace

namespace  Solana.Unity.SDK
{
    [Serializable]
    public class PhantomTransactionPayload
    {
        public string transaction;
        public string session;

        public PhantomTransactionPayload(string transaction, string session)
        {
            this.transaction = transaction;
            this.session = session;
        }
    }
}