import { useRef, useState } from 'react';
import api from '../api/client';

interface ImageUploadProps {
  value: string;
  onChange: (url: string) => void;
  uploadEndpoint?: string;
  label?: string;
}

export default function ImageUpload({ value, onChange, uploadEndpoint = '/api/upload/image', label = 'Hình ảnh' }: ImageUploadProps) {
  const fileRef = useRef<HTMLInputElement>(null);
  const [uploading, setUploading] = useState(false);

  const handleFile = async (e: React.ChangeEvent<HTMLInputElement>) => {
    const file = e.target.files?.[0];
    if (!file) return;
    setUploading(true);
    try {
      const formData = new FormData();
      formData.append('file', file);
      const res = await api.post(uploadEndpoint, formData, {
        headers: { 'Content-Type': 'multipart/form-data' }
      });
      onChange(res.data.url || res.data.imageUrl || '');
    } catch {
      // Fallback: use local ObjectURL for preview only
      onChange(URL.createObjectURL(file));
    } finally {
      setUploading(false);
    }
  };

  return (
    <div>
      <label className="form-label small text-secondary">{label}</label>
      <div className="d-flex gap-2 align-items-center">
        <input
          type="text"
          className="form-control"
          placeholder="URL hình ảnh hoặc tải lên"
          value={value}
          onChange={e => onChange(e.target.value)}
        />
        <button
          type="button"
          className="btn btn-outline-secondary btn-sm d-flex align-items-center gap-1 flex-shrink-0"
          onClick={() => fileRef.current?.click()}
          disabled={uploading}
          style={{ whiteSpace: 'nowrap' }}
        >
          {uploading
            ? <span className="spinner-border spinner-border-sm"></span>
            : <i className="fas fa-cloud-upload-alt"></i>}
          Tải lên
        </button>
        <input ref={fileRef} type="file" accept="image/*" hidden onChange={handleFile} />
      </div>
      {value && (
        <div className="mt-2" style={{ maxWidth: 120 }}>
          <img src={value} alt="preview" className="rounded"
            style={{ width: '100%', height: 80, objectFit: 'cover', border: '1px solid var(--border-color)' }}
            onError={e => (e.currentTarget.style.display = 'none')} />
        </div>
      )}
    </div>
  );
}
