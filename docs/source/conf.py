# Configuration file for the Sphinx documentation builder.

# -- Project information

project = 'MediatR'
copyright = 'Lucky Penny Software'
author = 'Jimmy Bogard'

# -- General configuration

extensions = [
    'myst_parser'
]

# Generate anchors for headings so pages can link to sections, such as #client-redistribution-scenarios
myst_heading_anchors = 3

# -- Options for HTML output

html_theme = 'sphinx_rtd_theme'
html_theme_options = {
    'logo_only': True
}
html_logo = 'img/logo.png'

# -- Options for EPUB output

epub_show_urls = 'footnote'
