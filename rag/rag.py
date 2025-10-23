import PyPDF2
import os
import chromadb
from sentence_transformers import SentenceTransformer
from datetime import datetime  # ✅ Added missing import


# Extract text from the pdf
def extract_text_from_pdf(pdf_path):
    text = ""
    with open(pdf_path, 'rb') as file:
        pdf_reader = PyPDF2.PdfReader(file)
        for page in pdf_reader.pages:
            text += page.extract_text() + "\n"
    return text


# Load documents
ipc_text = extract_text_from_pdf("ipc codes.pdf")
constitution_text = extract_text_from_pdf("constitution.pdf")


# Chunking function
def legal_document_chunker(text, max_length=800, overlap=100):
    """Legal-specific chunking that preserves section structure"""
    chunks = []

    separators = [
        "\n\nSection ",  # IPC sections
        "\nArticle ",  # Constitution articles
        "\nChapter ",  # Chapter breaks
        "\n\n",  # Paragraph breaks
        "\n",  # Line breaks
        ". ",  # Sentence breaks
        " ",  # Word breaks
        ""  # Character breaks
    ]

    def recursive_split(text, max_len):
        if len(text) <= max_len:
            return [text.strip()]

        for separator in separators:
            if separator in text:
                parts = text.split(separator)
                result = []
                current_chunk = ""

                for i, part in enumerate(parts):
                    test_chunk = current_chunk + separator + part if current_chunk else part

                    if len(test_chunk) <= max_len:
                        current_chunk = test_chunk
                    else:
                        if current_chunk:
                            result.extend(recursive_split(current_chunk, max_len))
                        current_chunk = part

                if current_chunk:
                    result.extend(recursive_split(current_chunk, max_len))
                return result

        return [text[:max_len], text[max_len:]]

    raw_chunks = recursive_split(text, max_length)

    for i in range(len(raw_chunks)):
        chunk = raw_chunks[i]
        if i > 0 and len(raw_chunks[i - 1]) > overlap:
            prev_overlap = raw_chunks[i - 1][-overlap:]
            chunk = prev_overlap + " ... " + chunk
        chunks.append(chunk.strip())

    return chunks


# Chunk documents
ipc_chunks = legal_document_chunker(ipc_text)
constitution_chunks = legal_document_chunker(constitution_text)


# ✅ Fixed: Moved function outside of class
def get_legal_context_for_query(query, rag_system, max_context_length=2000):
    """Get relevant legal context for a query to feed into your fine-tuned model"""
    results = rag_system.search_legal_context(query, n_results=3)

    context_pieces = []
    current_length = 0

    for doc, metadata in zip(results['documents'], results['metadatas']):
        if current_length + len(doc) <= max_context_length:
            source_info = f"[{metadata['doc_type']} - {metadata['source']}]"
            context_pieces.append(f"{source_info}\n{doc}")
            current_length += len(doc)
        else:
            break

    return "\n\n---\n\n".join(context_pieces)


# Vector database class
class LegalRAGSystem:
    def __init__(self, collection_name="dikastirio_legal_kb"):
        self.client = chromadb.Client()

        try:
            self.client.delete_collection(collection_name)
        except:
            pass

        self.collection = self.client.create_collection(
            name=collection_name,
            metadata={"description": "Legal knowledge base for Dikastirio VR courtroom"}
        )

        self.embedding_model = SentenceTransformer('all-MiniLM-L6-v2')
        print(f"✅ ChromaDB collection '{collection_name}' initialized")

    def add_documents(self, chunks, doc_type, source_file):
        """Add document chunks to vector database"""
        documents = []
        metadatas = []
        ids = []

        for i, chunk in enumerate(chunks):
            if len(chunk.strip()) < 50:
                continue

            documents.append(chunk)
            metadatas.append({
                "doc_type": doc_type,
                "source": source_file,
                "chunk_id": i,
                "length": len(chunk)
            })
            ids.append(f"{doc_type}_{i}")

        self.collection.add(
            documents=documents,
            metadatas=metadatas,
            ids=ids
        )

        print(f"✅ Added {len(documents)} chunks from {doc_type}")

    def search_legal_context(self, query, n_results=5):
        """Search for relevant legal context"""
        results = self.collection.query(
            query_texts=[query],
            n_results=n_results
        )

        return {
            'documents': results['documents'][0],
            'metadatas': results['metadatas'][0],
            'distances': results['distances'][0] if 'distances' in results else None
        }


# Enhanced RAG class
class EnhancedLegalRAG(LegalRAGSystem):
    def __init__(self, collection_name="dikastirio_legal_enhanced"):
        super().__init__(collection_name)

        try:
            self.embedding_model = SentenceTransformer('Stern5497/sbert-legal-xlm-roberta-base')
            print("✅ Using legal-specific embedding model")
        except:
            print("⚠️  Legal model not available, using general model")
            self.embedding_model = SentenceTransformer('all-MiniLM-L6-v2')


# ✅ Fixed: Made this a standalone function
def query_legal_system(user_question, rag_system, fine_tuned_model=None):
    """Complete RAG pipeline for legal queries"""
    legal_context = get_legal_context_for_query(user_question, rag_system)

    prompt = f"""
Context from Legal Documents:
{legal_context}

Question: {user_question}

Please provide a detailed legal answer based on the provided context:
"""

    return {
        'context_used': legal_context,
        'prompt': prompt,
        'sources': [meta['source'] for meta in rag_system.search_legal_context(user_question, 3)['metadatas']]
    }


# Dikastirio Assistant class
class DikastirioLegalAssistant:
    def __init__(self):
        self.rag_system = LegalRAGSystem()
        # Add documents to the system
        self.rag_system.add_documents(ipc_chunks, "IPC", "Indian_Penal_Code.pdf")
        self.rag_system.add_documents(constitution_chunks, "Constitution", "Constitution_of_India.pdf")

    def process_courtroom_query(self, query, context_type="general"):
        """Process legal queries in VR courtroom context"""
        enhanced_query = f"In a courtroom proceeding: {query}"
        context = get_legal_context_for_query(enhanced_query, self.rag_system)

        return {
            'legal_context': context,
            'query_type': context_type,
            'timestamp': datetime.now().isoformat()
        }


def main():

    print("Step 1: Loading legal documents...")
    ipc_text = extract_text_from_pdf("ipc codes.pdf")
    constitution_text = extract_text_from_pdf("constitution.pdf")
    print(" Documents loaded.")

    print("\nStep 2: Chunking documents...")
    ipc_chunks = legal_document_chunker(ipc_text)
    constitution_chunks = legal_document_chunker(constitution_text)
    print(" Documents chunked.")

    print("\nStep 3: Initializing the Enhanced RAG System...")
    # We use your best class with the specialized legal model
    rag_system = EnhancedLegalRAG()
    rag_system.add_documents(ipc_chunks, "IPC", "Indian_Penal_Code.pdf")
    rag_system.add_documents(constitution_chunks, "Constitution", "Constitution_of_India.pdf")
    print(" RAG System is ready.")

    # --- Ask Your Single Question Here ---
    user_question = "What constitutes criminal conspiracy under Indian law?"
    print(f"\nStep 4: Processing the query: '{user_question}'")

    # This one function call runs the core RAG logic
    final_result = query_legal_system(user_question, rag_system)
    print(" Query processed.")

    # --- Get the Final Answer ---
    print("\n\n--- FINAL RESULT ---")
    print("\n Sources Retrieved:")
    for source in final_result['sources']:
        print(f" - {source}")

    print("\n📝 Final Prompt for the Fine-Tuned Model:")
    print("-----------------------------------------")
    print(final_result['prompt'])
    print("-----------------------------------------")


if __name__ == "__main__":
    main()