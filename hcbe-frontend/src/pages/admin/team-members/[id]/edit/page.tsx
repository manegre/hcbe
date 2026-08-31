import React, { useState, useEffect, useRef } from 'react';
import { useParams, useNavigate } from 'react-router-dom';
import { AdminFormLayout } from '../../../../../components/admin/AdminFormLayout';
import { Button, Field, inputClasses } from '../../../../../components/ui';
import { teamMembersApi } from '../../../../../lib/api/team-members';
import type { UpdateTeamMemberRequest } from '../../../../../lib/api/types';

const TeamMemberEditPage: React.FC = () => {
  const { id } = useParams<{ id: string }>();
  const navigate = useNavigate();
  const formRef = useRef<HTMLFormElement>(null);
  const [loading, setLoading] = useState(true);
  const [submitting, setSubmitting] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [formData, setFormData] = useState<UpdateTeamMemberRequest>({
    name: '',
    position: '',
    positionEn: '',
    region: '',
    regionEn: '',
    zone: '',
    zoneEn: '',
    photo: '',
    bio: '',
    bioEn: '',
    email: '',
    order: 0,
    isActive: true
  });

  const backPath = `/admin/team-members/${id}`;

  useEffect(() => {
    const loadMember = async () => {
      if (!id) return;

      try {
        setLoading(true);
        const response = await teamMembersApi.getTeamMemberById(id);
        if (response.success && response.data) {
          const member = response.data;
          setFormData({
            name: member.name,
            position: member.position,
            positionEn: member.positionEn || '',
            region: member.region,
            regionEn: member.regionEn || '',
            zone: member.zone,
            zoneEn: member.zoneEn || '',
            photo: member.photo || '',
            bio: member.bio || '',
            bioEn: member.bioEn || '',
            email: member.email || '',
            order: member.order,
            isActive: member.isActive
          });
        } else {
          setError('Failed to load team member');
        }
      } catch (err) {
        console.error('Error loading team member:', err);
        setError('Error loading team member');
      } finally {
        setLoading(false);
      }
    };

    loadMember();
  }, [id]);

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!id) return;

    setSubmitting(true);
    setError(null);

    try {
      const response = await teamMembersApi.updateTeamMember(id, formData);
      if (response.success) {
        navigate(`/admin/team-members/${id}`);
      } else {
        setError('Failed to update team member');
      }
    } catch (err) {
      console.error('Error updating team member:', err);
      setError('Error updating team member');
    } finally {
      setSubmitting(false);
    }
  };

  const handleChange = (e: React.ChangeEvent<HTMLInputElement | HTMLTextAreaElement | HTMLSelectElement>) => {
    const { name, value, type } = e.target;
    setFormData(prev => ({
      ...prev,
      [name]: type === 'number' ? parseInt(value) :
              type === 'checkbox' ? (e.target as HTMLInputElement).checked :
              value
    }));
  };

  if (loading) {
    return (
      <div className="flex justify-center py-12">
        <div className="h-8 w-8 animate-spin border-2 border-line border-t-green" />
      </div>
    );
  }

  return (
    <form ref={formRef} onSubmit={handleSubmit} className="min-w-0">
      <AdminFormLayout
        title="Edit Team Member"
        backPath={backPath}
        backLabel="Cancel"
        onCancel={() => navigate(backPath)}
        onSave={() => formRef.current?.requestSubmit()}
        actions={
          <Button type="submit" variant="primary" disabled={submitting}>
            {submitting ? 'Saving...' : 'Save Changes'}
          </Button>
        }
        main={
          <div>
            {error && (
              <p className="mb-6 border border-error bg-surface px-4 py-3 text-error">{error}</p>
            )}
            <div className="grid grid-cols-1 gap-6 md:grid-cols-2">
              <div className="md:col-span-2">
                <Field label="Name" htmlFor="name" required>
                  <input
                    type="text"
                    id="name"
                    name="name"
                    value={formData.name}
                    onChange={handleChange}
                    required
                    className={inputClasses}
                  />
                </Field>
              </div>

              <div className="md:col-span-2">
                <Field label="Position" htmlFor="position" required>
                  <input
                    type="text"
                    id="position"
                    name="position"
                    value={formData.position}
                    onChange={handleChange}
                    required
                    className={inputClasses}
                  />
                </Field>
              </div>

              <div className="md:col-span-2"><Field label="Position (English)" htmlFor="positionEn"><input type="text" id="positionEn" name="positionEn" value={formData.positionEn} onChange={handleChange} className={inputClasses} /></Field></div>

              <Field label="Region" htmlFor="region" required>
                <input
                  type="text"
                  id="region"
                  name="region"
                  value={formData.region}
                  onChange={handleChange}
                  required
                  className={inputClasses}
                />
              </Field>

              <Field label="Region (English)" htmlFor="regionEn"><input type="text" id="regionEn" name="regionEn" value={formData.regionEn} onChange={handleChange} className={inputClasses} /></Field>

              <Field label="Zone" htmlFor="zone" required>
                <select
                  id="zone"
                  name="zone"
                  value={formData.zone}
                  onChange={handleChange}
                  required
                  className={`${inputClasses} cursor-pointer`}
                >
                  <option value="">Select Zone</option>
                  <option value="Zone 1">Zone 1</option>
                  <option value="Zone 2">Zone 2</option>
                </select>
              </Field>

              <Field label="Zone (English)" htmlFor="zoneEn"><input type="text" id="zoneEn" name="zoneEn" value={formData.zoneEn} onChange={handleChange} className={inputClasses} /></Field>

              <div className="md:col-span-2">
                <Field label="Email" htmlFor="email">
                  <input
                    type="email"
                    id="email"
                    name="email"
                    value={formData.email}
                    onChange={handleChange}
                    className={inputClasses}
                  />
                </Field>
              </div>

              <div className="md:col-span-2">
                <Field label="Photo URL" htmlFor="photo">
                  <input
                    type="url"
                    id="photo"
                    name="photo"
                    value={formData.photo}
                    onChange={handleChange}
                    className={inputClasses}
                    placeholder="https://example.com/photo.jpg"
                  />
                </Field>
              </div>

              <div className="md:col-span-2">
                <Field label="Biography" htmlFor="bio">
                  <textarea
                    id="bio"
                    name="bio"
                    value={formData.bio}
                    onChange={handleChange}
                    rows={4}
                    className={inputClasses}
                  />
                </Field>
              </div>

              <div className="md:col-span-2"><Field label="Biography (English)" htmlFor="bioEn"><textarea id="bioEn" name="bioEn" value={formData.bioEn} onChange={handleChange} rows={4} className={inputClasses} /></Field></div>

              <Field label="Display Order" htmlFor="order" required>
                <input
                  type="number"
                  id="order"
                  name="order"
                  value={formData.order}
                  onChange={handleChange}
                  required
                  min="0"
                  className={inputClasses}
                />
              </Field>

              <div className="flex items-center">
                <label htmlFor="isActive" className="flex min-h-[44px] cursor-pointer items-center gap-3">
                  <input
                    type="checkbox"
                    id="isActive"
                    name="isActive"
                    checked={formData.isActive}
                    onChange={handleChange}
                    className="h-5 w-5 rounded-control-sm border border-outline accent-green"
                  />
                  <span className="text-body-md text-ink">Active</span>
                </label>
              </div>
            </div>
          </div>
        }
      />
    </form>
  );
};

export default TeamMemberEditPage;
