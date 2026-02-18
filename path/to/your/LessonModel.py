class Lesson(models.Model):
    title = models.CharField(max_length=200)
    description = models.TextField()
    canva_url = models.URLField(max_length=200, blank=True, null=True)  # New field for storing Canva canvas URLs
    # Other fields...
    
    def __str__(self):
        return self.title